﻿using AspNetCore.Http.Extensions;
using Fb.MC.Certificate;
using Fb.MC.Views;
using Flatbuilder.DTO;
using FreshMvvm;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Fb.MC.Views
{
    public class LoginPageModel : FreshBasePageModel
    {
        static readonly Uri baseAddress = new Uri("https://10.0.2.2:5001/");

        public ICommand LoginCommand { private set; get; }
        public ICommand CreateCommand { private set; get; }
        private string userName;
        public string UserName
        {
            get
            {
                return userName;
            }
            set
            {
                userName = value;
                RaisePropertyChanged("userName");
                ((Command)LoginCommand).ChangeCanExecute();
                // PropertyChanged(this, new PropertyChangedEventArgs("username")); 
            }
        }
        private string password;
        public string Password
        {
            get
            {
                return password;
            }
            set
            {
                password = value;
                RaisePropertyChanged("password");
                ((Command)LoginCommand).ChangeCanExecute();
                // PropertyChanged(this, new PropertyChangedEventArgs("username")); 
            }
        }
        
        private bool error = false;
        public bool Error
        {
            get
            {
                return error;
            }
            set
            {
                error = value;
                RaisePropertyChanged("error");
                RaisePropertyChanged("Error");
            }
        }
        public LoginPageModel()
        {
            CreateCommand = new Command(

                execute: async () =>
                {
                    var navpage = new FreshNavigationContainer(FreshPageModelResolver.ResolvePageModel<CreateCustomerPageModel>());
                    Application.Current.MainPage = navpage;
                }
            );

            LoginCommand = new Command(
               execute: async () =>
                {
                    Costumer costumer;
                    HttpClient httpClient;
                    switch (Device.RuntimePlatform)
                    {
                        case Device.Android:
                            httpClient = new HttpClient(DependencyService.Get<IHTTPClientHandlerCreationService>().GetInsecureHandler());
                            break;
                        default:
                            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                            httpClient = new HttpClient(new HttpClientHandler());
                            break;
                    }
                    using (httpClient)
                    {
                        httpClient.BaseAddress = baseAddress;

                        costumer = new Costumer() { Name = UserName, Password = Password };

                        var response = await httpClient.PostAsJsonAsync<Costumer>("api/Customer/customerLogin", costumer);

                        if(response.StatusCode == HttpStatusCode.OK)
                        {
                            string json = await response.Content.ReadAsStringAsync();
                            costumer = JsonConvert.DeserializeObject<Costumer>(json);
                        }
                        else
                        {
                            Error = true;
                            return;
                        }
                    }
                    var navpage = new FreshNavigationContainer(FreshPageModelResolver.ResolvePageModel<MainPageModel>(costumer));
                    Application.Current.MainPage = navpage;
                },
               canExecute: () =>
                {
                    if (UserName == "" ||
                        UserName == null ||
                        Password == "" ||
                        Password == null)
                    {
                        return false;
                    }
                    else return true;
                });
        }

        public override void Init(object initData)
        {
            base.Init(initData);

        }
    }
}
