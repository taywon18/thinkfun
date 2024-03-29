﻿using System.Net.Http.Json;
using ThinkFun.Model;

namespace ThinkFun;

public class LoginManager
{
    const string API_USER_GETLOCALUSER = "User/GetLocalUser";
    const string API_USER_LOGIN = "User/Login";

    public static LoginManager Instance { get; } = new LoginManager();
    private HttpClient Client
    {
        get => DataManager.Instance.Client;
    }

    public User? LastUser = null;

    public async Task<bool> CheckIsConnected(CancellationToken tk = default)
    {
        try
        {
            var user = await Client.GetFromJsonAsync<Model.User>(API_USER_GETLOCALUSER, tk);

            LastUser = user;
            DataManager.Instance.SaveCookies();

            return true;
        } catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<User?> Login(string name, string password, CancellationToken tk = default)
    {
        try
        {
            var ret = await Client.PostAsJsonAsync<Model.LoginRequest>(API_USER_LOGIN, new LoginRequest
            {
                Name = name,
                Password = password
            });

            if (!ret.IsSuccessStatusCode)
                return null;

            LastUser = await ret.Content.ReadFromJsonAsync<User>();
            DataManager.Instance.SaveCookies();
            return LastUser;


        }
        catch (Exception ex)
        {
            return null;
        }
    }

}
