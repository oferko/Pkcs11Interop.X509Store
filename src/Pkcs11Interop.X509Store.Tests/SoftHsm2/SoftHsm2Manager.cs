﻿/*
 *  Copyright 2017 The Pkcs11Interop Project
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

/*
 *  Written for the Pkcs11Interop project by:
 *  Jaroslav IMRICH <jimrich@jimrich.sk>
 */

using System;
using System.Collections.Generic;
using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;

namespace Net.Pkcs11Interop.X509Store.Tests.SoftHsm2
{
    public static class SoftHsm2Manager
    {
        public const string Token1Label = "First token";

        public const string Token1SoPin = "1111111111";

        public const string Token1UserPin = "11111111";

        public const string Token1TestCaLabel = "TestCa";

        public const string Token1TestUserRsaLabel = "TestUserRsa";

        public const string Token1TestUserEcdsaLabel = "TestUserEcdsa";

        public const string Token2Label = "Second token";

        public const string Token2SoPin = "2222222222";

        public const string Token2UserPin = "22222222";

        private static string _libraryPath = null;

        public static string LibraryPath
        {
            get
            {
                return _libraryPath;
            }
        }

        private static IPinProvider _pinProvider = null;

        public static IPinProvider PinProvider
        {
            get
            {
                if (_pinProvider == null)
                    _pinProvider = new SoftHsm2PinProvider();

                return _pinProvider;
            }

        }

        static SoftHsm2Manager()
        {
            // Setup environment variable with path to configuration file
            Environment.SetEnvironmentVariable("SOFTHSM2_CONF", @"Pkcs11Interop.X509Store.Tests\SoftHsm2\softhsm2.conf");

            // Determine path to PKCS#11 library
            if (Platform.Uses64BitRuntime)
                _libraryPath = @"Pkcs11Interop.X509Store.Tests\SoftHsm2\softhsm2-x64.dll";
            else
                _libraryPath = @"Pkcs11Interop.X509Store.Tests\SoftHsm2\softhsm2.dll";

            InitializeTokens();
        }

        private static void InitializeTokens()
        {
            // Initialize tokens and import objects
            using (var pkcs11 = new Pkcs11(LibraryPath, AppType.MultiThreaded))
            {
                // Initialize first token
                List<Slot> slots = pkcs11.GetSlotList(SlotsType.WithOrWithoutTokenPresent);
                if (slots.Count != 1)
                    return; // Already initialized
                else
                    InitializeToken(slots[0], Token1Label, Token1SoPin, Token1UserPin);

                // Initialize second token
                slots = pkcs11.GetSlotList(SlotsType.WithOrWithoutTokenPresent);
                if (slots.Count != 2)
                    throw new Exception("Unexpected number of slots");
                else
                    InitializeToken(slots[1], Token2Label, Token2SoPin, Token2UserPin);

                // Import objects to first token
                using (Session session = slots[0].OpenSession(SessionType.ReadWrite))
                {
                    session.Login(CKU.CKU_USER, Token1UserPin);

                    // Import CA cert without private key
                    session.CreateObject(CryptoObjects.GetTestCaCertAttributes(Token1TestCaLabel));

                    // Import user cert with RSA private and public keys
                    session.CreateObject(CryptoObjects.GetTestUserRsaCertAttributes(Token1TestUserRsaLabel));
                    session.CreateObject(CryptoObjects.GetTestUserRsaPrivKeyAttributes(Token1TestUserRsaLabel));
                    session.CreateObject(CryptoObjects.GetTestUserRsaPubKeyAttributes(Token1TestUserRsaLabel));

                    // Import user cert with ECDSA private and public keys
                    session.CreateObject(CryptoObjects.GetTestUserEcdsaCertAttributes(Token1TestUserEcdsaLabel));
                    session.CreateObject(CryptoObjects.GetTestUseEcdsaPrivKeyAttributes(Token1TestUserEcdsaLabel));
                    session.CreateObject(CryptoObjects.GetTestUseEcdsaPubKeyAttributes(Token1TestUserEcdsaLabel));
                }
            }
        }

        private static void InitializeToken(Slot slot, string label, string soPin, string userPin)
        {
            if (slot.GetTokenInfo().TokenFlags.TokenInitialized)
                throw new Exception("Token already initialized");

            slot.InitToken(soPin, label);
            using (Session session = slot.OpenSession(SessionType.ReadWrite))
            {
                session.Login(CKU.CKU_SO, soPin);
                session.InitPin(userPin);
            }
        }
    }
}
