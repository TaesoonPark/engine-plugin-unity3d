﻿// Copyright 2018 iFunFactory Inc. All Rights Reserved.
//
// This work is confidential and proprietary to iFunFactory Inc. and
// must not be used, disclosed, copied, or distributed without the prior
// consent of iFunFactory Inc.

using Fun;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;


public class TestReconnect
{
    [UnityTest]
    public IEnumerator TCP_Json ()
    {
        yield return new TestImpl (TransportProtocol.kTcp, FunEncoding.kJson);
    }

    [UnityTest]
    public IEnumerator TCP_Protobuf ()
    {
        yield return new TestImpl (TransportProtocol.kTcp, FunEncoding.kProtobuf);
    }


    class TestImpl : TestSessionBase
    {
        public TestImpl (TransportProtocol protocol, FunEncoding encoding)
        {
            SessionOption option = new SessionOption();
            option.sessionReliability = true;

            session = FunapiSession.Create(TestInfo.ServerIp, option);
            session.ReceivedMessageCallback += onReceivedEchoMessage;

            session.TransportEventCallback += delegate (TransportProtocol p, TransportEventType type)
            {
                if (isFinished)
                    return;

                if (type == TransportEventType.kStarted)
                {
                    startCoroutine(onStarted(protocol));
                }
                else if (type == TransportEventType.kStopped)
                {
                    startCoroutine(onStopped(protocol));
                }
            };

            setTestTimeout(3f);

            ushort port = getPort("whole", protocol, encoding);
            session.Connect(protocol, encoding, port);
        }

        IEnumerator onStarted (TransportProtocol protocol)
        {
            sendEchoMessageWithCount(protocol, 3);

            yield return new SleepForSeconds(0.2f);

            session.Stop();
        }

        IEnumerator onStopped (TransportProtocol protocol)
        {
            ++test_step;
            if (test_step >= kStepCountMax)
            {
                onTestFinished();
                yield break;
            }

            yield return new SleepForSeconds(0.2f);

            session.Connect(protocol);
        }


        const int kStepCountMax = 3;
        int test_step = 0;
    }
}
