#include <stdafx.h>

#include "OnlineVoting.h"

#define BUFFER_SIZE 256
#define VOTING_PROXY_START_ARGS LPSTR("chaosmod\\OnlineVotingApp.exe --startServer")

OnlineVoting::OnlineVoting(const std::array<BYTE, 3>& rgTextColor) : m_rgTextColor(rgTextColor)
{
	m_bOnlineVotingEnabled = g_OptionsManager.GetOnlineValue<bool>("OnlineEnabled", OPTION_ONLINE_ENABLED);

	if (!m_bOnlineVotingEnabled)
	{
		return;
	}

	if (g_EnabledEffects.size() < 5)
	{
		ErrorOutWithMsg("You need at least 5 enabled effects to enable Online voting. Reverting to normal mode.");

		return;
	}

	m_bRandomEffectVotableOnline = g_OptionsManager.GetOnlineValue<bool>("RandomEffectVotableOnline", OPTION_ONLIINE_RANDOM_EFFECT);

	g_pEffectDispatcher->m_bDispatchEffectsOnTimer = false;

	STARTUPINFO startupInfo = {};
	PROCESS_INFORMATION procInfo = {};

#ifdef _DEBUG
	DWORD ulAttributes = NULL;
	if (DoesFileExist("chaosmod\\.forcenovotingconsole"))
	{
		ulAttributes = CREATE_NO_WINDOW;
	}

	bool bResult = CreateProcess(NULL, VOTING_PROXY_START_ARGS, NULL, NULL, TRUE, ulAttributes, NULL, NULL, &startupInfo, &procInfo);
#else
	bool bResult = CreateProcess(NULL, VOTING_PROXY_START_ARGS, NULL, NULL, TRUE, CREATE_NO_WINDOW, NULL, NULL, &startupInfo, &procInfo);
#endif

	// A previous instance of the voting proxy could still be running, wait for it to release the mutex
	HANDLE hMutex = OpenMutex(SYNCHRONIZE, FALSE, "ChaosModVVotingMutex");
	if (hMutex)
	{
		WaitForSingleObject(hMutex, INFINITE);
		ReleaseMutex(hMutex);
		CloseHandle(hMutex);
	}

	if (!bResult)
	{
		ErrorOutWithMsg((std::ostringstream() << "Error while starting chaosmod/OnlineVotingApp.exe (Error Code: " << GetLastError() << "). Please verify the file exists. Reverting to normal mode.").str());

		return;
	}

	m_hPipeHandle = CreateNamedPipe("\\\\.\\pipe\\ChaosModVOnlineVotePipe", PIPE_ACCESS_DUPLEX, PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_NOWAIT,
		1, BUFFER_SIZE, BUFFER_SIZE, 0, NULL);

	if (m_hPipeHandle == INVALID_HANDLE_VALUE)
	{
		ErrorOutWithMsg("Error while creating a named pipe, previous instance of voting app might be running. Try reloading the mod. Reverting to normal mode.");

		return;
	}

	ConnectNamedPipe(m_hPipeHandle, NULL);
}

OnlineVoting::~OnlineVoting()
{
	if (m_hPipeHandle != INVALID_HANDLE_VALUE)
	{
		FlushFileBuffers(m_hPipeHandle);
		DisconnectNamedPipe(m_hPipeHandle);
		CloseHandle(m_hPipeHandle);
	}
}

void OnlineVoting::Run()
{
	if (!m_bOnlineVotingEnabled)
	{
		return;
	}
	// Check if there's been no ping for too long and error out
	DWORD64 ullCurTick = GetTickCount64();
	if (m_ullLastPing < ullCurTick - 1000)
	{
		if (m_iNoPingRuns == 5)
		{
			ErrorOutWithMsg("Connection to OnlineVotingApp aborted. Returning to normal mode.");

			return;
		}

		m_iNoPingRuns++;
		m_ullLastPing = ullCurTick;
	}

	char cBuffer[BUFFER_SIZE];
	DWORD ulBytesRead;
	if (!ReadFile(m_hPipeHandle, cBuffer, BUFFER_SIZE, &ulBytesRead, NULL))
	{
		while (GetLastError() == ERROR_IO_PENDING)
		{
			WAIT(0);
		}
	}

	if (ulBytesRead > 0)
	{
		if (!HandleMsg(std::string(cBuffer)))
		{
			return;
		}
	}

	if (!m_bReceivedHello)
	{
		return;
	}

	if (g_pEffectDispatcher->GetRemainingTimerTime() <= 1
		&& !m_bHasReceivedResult)
	{
		
		// Get vote result 1 second before effect is supposed to dispatch

		if (m_bIsVotingRunning)
		{
			m_bIsVotingRunning = false;

			if (!m_bNoVoteRound)
			{
				SendToPipe("getvoteresult");
			}
			else m_bHasReceivedResult = true;
		}
	}
	else if (g_pEffectDispatcher->ShouldDispatchEffectNow())
	{

		LOG("Ending voting round");
		// End of voting round; dispatch resulted effect

		if (m_bNoVoteRound)
		{
			g_pEffectDispatcher->DispatchRandomEffect();
			g_pEffectDispatcher->ResetTimer();

			if (!m_bOnlineVotingEnabled)
			{
				m_bNoVoteRound = false;
			}
		}
		else
		{
			// Should be random effect voteable, so just dispatch random effect
			if (m_pChosenEffectIdentifier->GetEffectType() == EFFECT_INVALID
				&& m_pChosenEffectIdentifier->GetScriptId().empty())
			{
				g_pEffectDispatcher->DispatchRandomEffect();
			}
			else
			{
				g_pEffectDispatcher->DispatchEffect(*m_pChosenEffectIdentifier);
			}
			g_pEffectDispatcher->ResetTimer();
		}

		if (MetaModifiers::m_ucAdditionalEffectsToDispatch > 0)
		{
			for (int i = 0; i < MetaModifiers::m_ucAdditionalEffectsToDispatch; i++)
			{
				g_pEffectDispatcher->DispatchRandomEffect();
			}
		}

		m_bIsVotingRoundDone = true;
	}
	else if (!m_bIsVotingRunning
		&& m_bReceivedFirstPing
		&& m_bIsVotingRoundDone)
	{
		// New voting round

		m_bIsVotingRunning = true;
		m_bHasReceivedResult = false;
		m_bIsVotingRoundDone = false;

		m_pChosenEffectIdentifier = std::make_unique<EffectIdentifier>();
		if (m_bNoVoteRound)
		{
			SendToPipe("novoteround");

			return;
		}

		m_rgEffectChoices.clear();
		std::unordered_map<EffectIdentifier, EffectData, EffectsIdentifierHasher> dictChoosableEffects;
		for (auto& pair : g_EnabledEffects)
		{
			auto& [effectIdentifier, effectData] = pair;

			if (effectData.TimedType != EEffectTimedType::Permanent
				&& !effectData.IsMeta()
				&& !effectData.ExcludedFromVoting()
				&& !effectData.IsUtility())
			{
				dictChoosableEffects.emplace(effectIdentifier, effectData);
			}
		}

		for (int idx = 0; idx < 4; idx++)
		{
			// 4th voteable is for random effect (if enabled)
			if (idx == 3 && m_bRandomEffectVotableOnline)
			{
				m_rgEffectChoices.push_back(std::make_unique<ChoosableEffect>(EFFECT_INVALID, "Random Effect", !m_bAlternatedVotingRound
						? 4
						: 8));
				break;
			}

			float fTotalWeight = 0.f;
			for (const auto& pair : dictChoosableEffects)
			{
				const EffectData& effectData = pair.second;

				fTotalWeight += GetEffectWeight(effectData);
			}

			float fChosen = g_Random.GetRandomFloat(0.f, fTotalWeight);

			fTotalWeight = 0.f;

			std::unique_ptr<ChoosableEffect> pTargetChoice;

			for (auto& pair : dictChoosableEffects)
			{
				auto& [effectIdentifier, effectData] = pair;

				fTotalWeight += GetEffectWeight(effectData);

				if (fChosen <= fTotalWeight)
				{
					// Set weight of this effect 0, EffectDispatcher::DispatchEffect will increment it immediately by EffectWeightMult
					effectData.Weight = 0;

					pTargetChoice = std::make_unique<ChoosableEffect>(effectIdentifier, effectData.HasCustomName()
						? effectData.CustomName
						: effectData.Name,
						!m_bAlternatedVotingRound
						? idx + 1
						: m_bRandomEffectVotableOnline
						? idx + 5
						: idx + 4
						);

					break;
				}
			}

			EffectIdentifier effectIdentifier = pTargetChoice->m_EffectIdentifier;

			m_rgEffectChoices.push_back(std::move(pTargetChoice));
			dictChoosableEffects.erase(effectIdentifier);
		}

		std::ostringstream oss;
		oss << "vote";
		for (const auto& pChoosableEffect : m_rgEffectChoices)
		{
			oss << ":" << pChoosableEffect->m_szEffectName;
		}
		SendToPipe(oss.str());

		m_bAlternatedVotingRound = !m_bAlternatedVotingRound;
	}
}

_NODISCARD bool OnlineVoting::IsEnabled() const
{
	return m_bOnlineVotingEnabled;
}

bool OnlineVoting::HandleMsg(const std::string& szMsg)
{
	if (szMsg == "hello")
	{
		m_bReceivedHello = true;

		LOG("Received Hello from voting app");
	}
	else if (szMsg == "ping")
	{
		m_ullLastPing = GetTickCount64();
		m_iNoPingRuns = 0;
		m_bReceivedFirstPing = true;
	}
	else if (szMsg == "invalid_poll_dur")
	{
		ErrorOutWithMsg("Invalid duration. Duration has to be above 15 and at most 181 seconds to make use of the poll system. Returning to normal mode.");

		return false;
	}
	else if (szMsg._Starts_with("voteresult"))
	{
		int iResult = std::stoi(szMsg.substr(szMsg.find(":") + 1));

		m_bHasReceivedResult = true;

		// If random effect voteable (result == 3) won, dispatch random effect later
		m_pChosenEffectIdentifier = std::make_unique<EffectIdentifier>((iResult == 3 && m_bRandomEffectVotableOnline) ? EFFECT_INVALID : m_rgEffectChoices[iResult]->m_EffectIdentifier);
	}
	else if (szMsg._Starts_with("currentvotes"))
	{
		std::string szValuesStr = szMsg.substr(szMsg.find(":") + 1);

		int iSplitIndex = szValuesStr.find(":");
		for (int i = 0; ; i++)
		{
			const std::string& szSplit = szValuesStr.substr(0, iSplitIndex);

			Util::TryParse<int>(szSplit, m_rgEffectChoices[i]->m_iChanceVotes);

			szValuesStr = szValuesStr.substr(iSplitIndex + 1);

			if (iSplitIndex == szValuesStr.npos)
			{
				break;
			}

			iSplitIndex = szValuesStr.find(":");
		}
	}

	return true;
}

void OnlineVoting::SendToPipe(std::string&& szMsg)
{
	szMsg += "\n";
	WriteFile(m_hPipeHandle, szMsg.c_str(), szMsg.length(), NULL, NULL);
}

void OnlineVoting::ErrorOutWithMsg(const std::string&& szMsg)
{
	MessageBox(NULL, szMsg.c_str(), "ChaosModV Error", MB_OK | MB_ICONERROR);

	DisconnectNamedPipe(m_hPipeHandle);
	CloseHandle(m_hPipeHandle);
	m_hPipeHandle = INVALID_HANDLE_VALUE;

	g_pEffectDispatcher->m_bDispatchEffectsOnTimer = true;
	m_bOnlineVotingEnabled = false;
}