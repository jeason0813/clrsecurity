﻿// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Security.Cryptography;
using Security.Cryptography.X509Certificates;

namespace Microsoft.Security.Cryptography.X509Certificates.Test
{
    /// <summary>
    ///     Tests for the X509Certificate2 extension methods
    /// </summary>
    [TestClass]
    public sealed class X509Certificate2Tests
    {
        private static X509Certificate2 s_microsoftCert = MicrosoftCert;

        /// <summary>
        ///     Gets a well-known certificate loaded from a file
        /// </summary>
        private static X509Certificate2 MicrosoftCert
        {
            get
            {
                using (Stream certStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Microsoft.Security.Cryptography.Test.Properties.microsoft.cer"))
                {
                   byte[] cert = new byte[certStream.Length];
                   certStream.Read(cert, 0, cert.Length);
                   return new X509Certificate2(cert);
                }
            }
        }

        /// <summary>
        ///     Tests to ensure that HasCngKey returns false on an opened cert
        /// </summary>
        [TestMethod]
        public void HasCngKeyTestOpenedCert()
        {
            Assert.IsFalse(s_microsoftCert.HasCngKey());
        }

        /// <summary>
        ///     Tests to ensure HasCngKey returns true on a CNG certificate
        /// </summary>
        [TestMethod]
        public void HasCngKeyTestCngCert()
        {
            const string keyName = "Microsoft.Security.Cryptography.X509Certificates.Test.X509Certificate2Tests.HasCngKeyTest.RSA1";
            try
            {
                // Create a cert for a persisted CNG key
                CngKeyCreationParameters keyCreationParams = new CngKeyCreationParameters();
                keyCreationParams.ExportPolicy = CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport;
                using (CngKey key = CngKey.Create(CngAlgorithm2.Rsa, keyName, keyCreationParams))
                {
                    X509CertificateCreationParameters creationParams =
                        new X509CertificateCreationParameters(new X500DistinguishedName("CN=CngCert"));
                    creationParams.CertificateCreationOptions = X509CertificateCreationOptions.None;

                    // A CNG certificate using a named key which is linked to the cert itself should return true
                    X509Certificate2 cngCert = key.CreateSelfSignedCertificate(creationParams);
                    Assert.IsTrue(cngCert.HasCngKey());

                    // A CNG cert exported and then re-imported should also return true
                    byte[] pfx = cngCert.Export(X509ContentType.Pfx, "CngCertPassword");
                    X509Certificate2 cngCertImport = new X509Certificate2(pfx, "CngCertPassword");
                    Assert.IsTrue(cngCertImport.HasCngKey());

                    key.Delete();
                }

                // Ephemeral CNG keys will create self signed certificates that do not link back to a CNG key
                // in the certificate (since there is no persisted CNG key to link to). 
                using (CngKey key = CngKey.Create(CngAlgorithm2.Rsa))
                {
                    X509Certificate2 ephemeralCert = key.CreateSelfSignedCertificate(new X500DistinguishedName("CN=EphemeralCngCert"));
                    Assert.IsFalse(ephemeralCert.HasCngKey());
                }
            }
            finally
            {
                // Make sure to delete the persisted key so we're clean for the next run.
                if (CngKey.Exists(keyName))
                {
                    using (CngKey key = CngKey.Open(keyName))
                    {
                        key.Delete();
                    }
                }
            }
        }
    }
}