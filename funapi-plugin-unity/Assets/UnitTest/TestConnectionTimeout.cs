﻿// Copyright 2018 iFunFactory Inc. All Rights Reserved.
//
// This work is confidential and proprietary to iFunFactory Inc. and
// must not be used, disclosed, copied, or distributed without the prior
// consent of iFunFactory Inc.

using Fun;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;


public class TestConnectionTimeout
{
    [UnityTest]
    public IEnumerator TCP_Timeout ()
    {
        yield return new TestImpl (TransportProtocol.kTcp, FunEncoding.kProtobuf);
    }

    [UnityTest]
    public IEnumerator HTTP_Timeout ()
    {
        yield return new TestImpl (TransportProtocol.kHttp, FunEncoding.kJson);
    }


    class TestImpl : TestSessionBase
    {
        public TestImpl (TransportProtocol protocol, FunEncoding encoding)
        {
            TransportOption option = newTransportOption(protocol);
            option.ConnectionTimeout = 0.02f;

            session = FunapiSession.Create(TestInfo.ServerIp);

            session.TransportEventCallback += delegate (TransportProtocol p, TransportEventType type)
            {
                if (type == TransportEventType.kStarted)
                {
                    resetSendingCount();
                    sendEchoMessageWithCount(protocol, 3);
                }
                else if (type == TransportEventType.kStopped)
                {
                    TransportError.Type errorCode = session.GetLastError(protocol);
                    if (errorCode == TransportError.Type.kStartingFailed ||
                        errorCode == TransportError.Type.kSendingFailed ||
                        errorCode == TransportError.Type.kConnectionTimeout)
                    {
                        ++test_step;
                        if (test_step < kStepCountMax)
                        {
                            ushort port = getPort("default", protocol, encoding);
                            session.Connect(protocol, encoding, port, option);
                        }
                        else if (test_step == kStepCountMax)
                        {
                            option.ConnectionTimeout = 3f;
                            ushort port = getPort("default", protocol, encoding);
                            session.Connect(protocol, encoding, port, option);
                        }
                    }
                }
            };

            session.ReceivedMessageCallback += delegate (string type, object message)
            {
                onReceivedEchoMessage(type, message);

                if (isReceivedAllMessages)
                    onTestFinished();
            };

            setTestTimeout(3f);

            session.Connect(protocol, encoding, 80, option);
        }


        const int kStepCountMax = 3;
        int test_step = 0;
    }
}
