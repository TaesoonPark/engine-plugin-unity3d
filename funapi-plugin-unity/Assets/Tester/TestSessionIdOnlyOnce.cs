﻿// Copyright 2018 iFunFactory Inc. All Rights Reserved.
//
// This work is confidential and proprietary to iFunFactory Inc. and
// must not be used, disclosed, copied, or distributed without the prior
// consent of iFunFactory Inc.

using Fun;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;


public class TestSessionIdOnlyOnce
{
    [UnityTest]
    public IEnumerator TCP_Json ()
    {
        yield return new TestImpl (TransportProtocol.kTcp, FunEncoding.kJson);
    }

    [UnityTest]
    public IEnumerator UDP_Json ()
    {
        yield return new TestImpl (TransportProtocol.kUdp, FunEncoding.kJson);
    }

    [UnityTest]
    public IEnumerator TCP_Protobuf ()
    {
        yield return new TestImpl (TransportProtocol.kTcp, FunEncoding.kProtobuf);
    }

    [UnityTest]
    public IEnumerator UDP_Protobuf ()
    {
        yield return new TestImpl (TransportProtocol.kUdp, FunEncoding.kProtobuf);
    }


    class TestImpl : TestSessionBase
    {
        public TestImpl (TransportProtocol protocol, FunEncoding encoding)
        {
            SessionOption option = new SessionOption();
            option.sendSessionIdOnlyOnce = true;

            if (protocol == TransportProtocol.kTcp)
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

            setTimeoutCallbackWithFail (5f, delegate ()
            {
                session.Stop();
            });

            ushort port = getPort("whole", protocol, encoding);
            session.Connect(protocol, encoding, port);
        }

        IEnumerator onStarted (TransportProtocol protocol)
        {
            if (protocol != TransportProtocol.kTcp)
                sendEchoMessageWithCount(protocol, 2);

            yield return new WaitForSeconds(0.2f);

            session.Stop();
        }

        IEnumerator onStopped (TransportProtocol protocol)
        {
            ++test_step;
            if (test_step == 1)
            {
                if (protocol == TransportProtocol.kTcp)
                    keepSendingEchoMessages(protocol, 0.1f);
            }
            else if (test_step >= kStepCountMax)
            {
                isFinished = true;
                yield break;
            }

            yield return new WaitForSeconds(0.2f);

            session.Connect(protocol);
        }


        const int kStepCountMax = 3;
        int test_step = 0;
    }
}