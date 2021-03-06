﻿using Fb.MC.Certificate;
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
    class MainPageModel : FreshBasePageModel
    {
        //public event PropertyChangedEventHandler PropertyChanged;

        static readonly Uri baseAddress = new Uri("https://10.0.2.2:5001/");

        public Order Selected { get; set; }

        private Costumer user;
        public Costumer User { get { return user; } private set {
                user = value;
                RaisePropertyChanged("User");
            } }

        private List<Order> orders;
        public List<Order> Orders
        {
            get
            {
                return orders;
            }
            set
            {
                orders = value;
                RaisePropertyChanged("Orders");
            }
        }

        public ICommand CreateOrderCommand { get; }
        public ICommand DetailsCommand { get; }
        public ICommand LogoutCommand { get; }

        public MainPageModel()
        {
            CreateOrderCommand = new Command(
            execute: async () =>
            {
                await CoreMethods.PushPageModel<NewOrderPageModel>(User);
            }
            );
            LogoutCommand = new Command(
                execute: async () =>
                {
                    (Application.Current).MainPage = new NavigationPage(new LoginPage());                    
                });
            DetailsCommand = new Command(
            execute: async (object param) =>
            {
                try
                {
                    if (Selected == null)
                        return;
                    await CoreMethods.PushPageModel<DetailsPageModel>(Selected);
                }
                catch (Exception e)
                {

                    throw;
                }
            }
            );
        }

        public override void Init(object initData)
        {
            base.Init(initData);
            User = (Costumer)initData;
        }

        protected override async void ViewIsAppearing(object sender, EventArgs e)
        {
            base.ViewIsAppearing(sender, e);
            Orders = await ListOrdersByName(User.Name);
        }

        public static async Task<List<Order>> ListOrdersByName(String name)
        {
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

           
                var response = await httpClient.GetAsync("api/Order/list/" + name);
                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Order>>(json);
            }
        }
    }
}
