﻿using System.Collections.Generic;
using System.Threading.Tasks;
using NsisoLauncherCore.Net.MojangApi;
using NsisoLauncherCore.Net.MojangApi.Api;
using static NsisoLauncherCore.Net.MojangApi.Responses.AuthenticateResponse;

namespace NsisoLauncherCore.Auth
{
    public interface IAuthenticator
    {
        Task<AuthenticateResult> DoAuthenticateAsync();
    }

    public class AuthenticateResult
    {
        public AuthenticateResult(AuthState state)
        {
            State = state;
        }

        public AuthState State { get; set; }

        public Error Error { get; set; }

        public string AccessToken { get; set; }

        public List<Uuid> Profiles { get; set; }

        public Uuid SelectedProfileUUID { get; set; }

        public UserData UserData { get; set; }
    }

    public enum AuthState
    {
        SUCCESS,
        REQ_LOGIN,
        ERR_INVALID_CRDL,
        ERR_NOTFOUND,
        ERR_METHOD_NOT_ALLOW,
        ERR_OTHER,
        ERR_INSIDE
    }
}