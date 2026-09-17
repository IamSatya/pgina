/*
	Copyright (c) 2013, pGina Team
	All rights reserved.

	Redistribution and use in source and binary forms, with or without
	modification, are permitted provided that the following conditions are met:
		* Redistributions of source code must retain the above copyright
		  notice, this list of conditions and the following disclaimer.
		* Redistributions in binary form must reproduce the above copyright
		  notice, this list of conditions and the following disclaimer in the
		  documentation and/or other materials provided with the distribution.
		* Neither the name of the pGina Team nor the names of its contributors 
		  may be used to endorse or promote products derived from this software without 
		  specific prior written permission.

	THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
	ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
	WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
	DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY
	DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
	(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
	LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
	ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
	(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
	SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace pGina.Plugin.Ldap
{
    enum HashMethod
    {
        PLAIN,
        SHA1,
        SSHA1,
        NT_HASH,
    }

    abstract class PasswordHashMethod
    {
        public string Name { get { return m_name; } }
        protected string m_name;

        public HashMethod Method { get { return m_method; } }
        protected HashMethod m_method;

        private static Random rand = new Random();

        public static Dictionary<HashMethod, PasswordHashMethod> methods;

        static PasswordHashMethod()
        {
            methods = new Dictionary<HashMethod, PasswordHashMethod>();
            methods.Add(HashMethod.PLAIN, new PasswordHashMethodPlain());
            methods.Add(HashMethod.SHA1, new PasswordHashMethodSHA1());
            methods.Add(HashMethod.SSHA1, new PasswordHashMethodSSHA1());
            methods.Add(HashMethod.NT_HASH, new PasswordHashMethodNTHash());
        }

        public abstract string hash(string pw);

        protected byte[] randomSalt(int len)
        {
            byte[] salt = new byte[len];

            for (int i = 0; i < len; i++)
            {
                salt[i] = (byte)rand.Next(256);
            }
            return salt;
        }
    }

    class PasswordHashMethodPlain : PasswordHashMethod
    {
        public PasswordHashMethodPlain()
        {
            m_name = "Plain Text";
            m_method = HashMethod.PLAIN;
        }

        public override string hash(string pw)
        {
            return pw;
        }
    }

    class PasswordHashMethodSHA1 : PasswordHashMethod
    {
        public PasswordHashMethodSHA1()
        {
            m_name = "SHA1";
            m_method = HashMethod.SHA1;
        }

        public override string hash(string pw)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] password = Encoding.UTF8.GetBytes(pw);
                byte[] hashData = sha1.ComputeHash(password);
                return "{SHA}" + Convert.ToBase64String(hashData);
            }
        }
    }

    class PasswordHashMethodSSHA1 : PasswordHashMethod
    {
        public PasswordHashMethodSSHA1()
        {
            m_name = "SSHA1 (Salted SHA1)";
            m_method = HashMethod.SSHA1;
        }

        public override string hash(string pw)
        {
            byte[] salt = randomSalt(8);
            byte[] password = Encoding.UTF8.GetBytes(pw);

            byte[] toHash = new byte[password.Length + salt.Length];
            Buffer.BlockCopy(password, 0, toHash, 0, password.Length);
            Buffer.BlockCopy(salt, 0, toHash, password.Length, salt.Length);

            byte[] hashData;
            using (SHA1 sha1 = SHA1.Create())
            {
                hashData = sha1.ComputeHash(toHash);
            }

            byte[] result = new byte[hashData.Length + salt.Length];
            Buffer.BlockCopy(hashData, 0, result, 0, hashData.Length);
            Buffer.BlockCopy(salt, 0, result, hashData.Length, salt.Length);

            return "{SSHA}" + Convert.ToBase64String(result);
        }
    }

    class PasswordHashMethodNTHash : PasswordHashMethod
    {
        public PasswordHashMethodNTHash()
        {
            m_name = "NT Hash (sambaNTPassword, MD4)";
            m_method = HashMethod.NT_HASH;
        }

        public override string hash(string pw)
        {
            byte[] password = Encoding.Unicode.GetBytes(pw);
            byte[] hashData = MD4.ComputeHash(password);
            return string.Concat(hashData.Select(b => b.ToString("X2")));
        }
    }

    /// <summary>
    /// RFC 1320 MD4 Message-Digest implementation.
    /// </summary>
    internal static class MD4
    {
        private static uint F(uint x, uint y, uint z) { return (x & y) | (~x & z); }
        private static uint G(uint x, uint y, uint z) { return (x & y) | (x & z) | (y & z); }
        private static uint H(uint x, uint y, uint z) { return x ^ y ^ z; }
        private static uint ROL(uint x, int n) { return (x << n) | (x >> (32 - n)); }

        private static void FF(ref uint a, uint b, uint c, uint d, uint x, int s) { a = ROL(a + F(b, c, d) + x, s); }
        private static void GG(ref uint a, uint b, uint c, uint d, uint x, int s) { a = ROL(a + G(b, c, d) + x + 0x5a827999U, s); }
        private static void HH(ref uint a, uint b, uint c, uint d, uint x, int s) { a = ROL(a + H(b, c, d) + x + 0x6ed9eba1U, s); }

        public static byte[] ComputeHash(byte[] input)
        {
            int len = input.Length;
            ulong bitLen = (ulong)len * 8;
            int padLen = (len % 64 < 56) ? (56 - len % 64) : (120 - len % 64);
            byte[] buf = new byte[len + padLen + 8];
            Buffer.BlockCopy(input, 0, buf, 0, len);
            buf[len] = 0x80;
            for (int i = 0; i < 8; i++)
            {
                buf[len + padLen + i] = (byte)((bitLen >> (i * 8)) & 0xff);
            }

            uint[] state = new uint[] { 0x67452301, 0xefcdab89, 0x98badcfe, 0x10325476 };
            uint[] x = new uint[16];

            for (int offset = 0; offset < buf.Length; offset += 64)
            {
                for (int i = 0; i < 16; i++)
                {
                    x[i] = (uint)buf[offset + i * 4] |
                           ((uint)buf[offset + i * 4 + 1] << 8) |
                           ((uint)buf[offset + i * 4 + 2] << 16) |
                           ((uint)buf[offset + i * 4 + 3] << 24);
                }

                uint a = state[0], b = state[1], c = state[2], d = state[3];

                FF(ref a, b, c, d, x[ 0], 3); FF(ref d, a, b, c, x[ 1], 7); FF(ref c, d, a, b, x[ 2], 11); FF(ref b, c, d, a, x[ 3], 19);
                FF(ref a, b, c, d, x[ 4], 3); FF(ref d, a, b, c, x[ 5], 7); FF(ref c, d, a, b, x[ 6], 11); FF(ref b, c, d, a, x[ 7], 19);
                FF(ref a, b, c, d, x[ 8], 3); FF(ref d, a, b, c, x[ 9], 7); FF(ref c, d, a, b, x[10], 11); FF(ref b, c, d, a, x[11], 19);
                FF(ref a, b, c, d, x[12], 3); FF(ref d, a, b, c, x[13], 7); FF(ref c, d, a, b, x[14], 11); FF(ref b, c, d, a, x[15], 19);

                GG(ref a, b, c, d, x[ 0], 3); GG(ref d, a, b, c, x[ 4], 5); GG(ref c, d, a, b, x[ 8], 9); GG(ref b, c, d, a, x[12], 13);
                GG(ref a, b, c, d, x[ 1], 3); GG(ref d, a, b, c, x[ 5], 5); GG(ref c, d, a, b, x[ 9], 9); GG(ref b, c, d, a, x[13], 13);
                GG(ref a, b, c, d, x[ 2], 3); GG(ref d, a, b, c, x[ 6], 5); GG(ref c, d, a, b, x[10], 9); GG(ref b, c, d, a, x[14], 13);
                GG(ref a, b, c, d, x[ 3], 3); GG(ref d, a, b, c, x[ 7], 5); GG(ref c, d, a, b, x[11], 9); GG(ref b, c, d, a, x[15], 13);

                HH(ref a, b, c, d, x[ 0], 3); HH(ref d, a, b, c, x[ 8], 9); HH(ref c, d, a, b, x[ 4], 11); HH(ref b, c, d, a, x[12], 15);
                HH(ref a, b, c, d, x[ 2], 3); HH(ref d, a, b, c, x[10], 9); HH(ref c, d, a, b, x[ 6], 11); HH(ref b, c, d, a, x[14], 15);
                HH(ref a, b, c, d, x[ 1], 3); HH(ref d, a, b, c, x[ 9], 9); HH(ref c, d, a, b, x[ 5], 11); HH(ref b, c, d, a, x[13], 15);
                HH(ref a, b, c, d, x[ 3], 3); HH(ref d, a, b, c, x[11], 9); HH(ref c, d, a, b, x[ 7], 11); HH(ref b, c, d, a, x[15], 15);

                state[0] += a;
                state[1] += b;
                state[2] += c;
                state[3] += d;
            }

            byte[] digest = new byte[16];
            for (int i = 0; i < 4; i++)
            {
                digest[i * 4] = (byte)(state[i] & 0xff);
                digest[i * 4 + 1] = (byte)((state[i] >> 8) & 0xff);
                digest[i * 4 + 2] = (byte)((state[i] >> 16) & 0xff);
                digest[i * 4 + 3] = (byte)((state[i] >> 24) & 0xff);
            }
            return digest;
        }
    }
}
