﻿using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using System;
using Plugin.Messaging;
using System.Linq;

namespace Customers
{
    public class CustomerListViewModel : BaseViewModel
    {
        public CustomerListViewModel()
        {
            DataSource = new CustomerDataSource();

            SubscribeToSaveCustomerMessages();

            SubscribeToDeleteCustomerMessages();
        }

        readonly IDataSource<Customer> DataSource;

        ObservableCollection<Customer> _Accounts;

        Command _LoadCustomersCommand;

        Command _CustomersRefreshCommand;

        Command _NewCustomerCommand;

        async Task FetchCustomers()
        {
            Accounts = new ObservableCollection<Customer>(await DataSource.GetItems(0, 1000));
        }

        public ObservableCollection<Customer> Accounts
        {
            get { return _Accounts; }
            set
            {
                _Accounts = value;
                OnPropertyChanged("Accounts");
            }
        }

        /// <summary>
        /// Command to load accounts
        /// </summary>
        public Command LoadCustomersCommand
        {
            get { return _LoadCustomersCommand ?? (_LoadCustomersCommand = new Command(async () => await ExecuteLoadCustomersCommand())); }
        }

        async Task ExecuteLoadCustomersCommand()
        {
            IsBusy = true;
            LoadCustomersCommand.ChangeCanExecute();

            await FetchCustomers();

            IsBusy = false;
            LoadCustomersCommand.ChangeCanExecute(); 
        }

        /// <summary>
        /// Command to create new customer
        /// </summary>
        public Command NewCustomerCommand
        {
            get
            {
                return _NewCustomerCommand ??
                (_NewCustomerCommand = new Command(async () =>
                        await ExecuteNewCustomerCommand()));
            }
        }

        async Task ExecuteNewCustomerCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            var page = new CustomerEditPage();

            var viewModel = new CustomerDetailViewModel() { Navigation = this.Navigation, Page = page };

            page.BindingContext = viewModel;

            await Navigation.PushAsync(page);

            IsBusy = false;
        }

        /// <summary>
        /// Command to fetch emote customers
        /// </summary>
        public Command CustomersRefreshCommand
        {
            get
            {
                return _CustomersRefreshCommand ??
                (_CustomersRefreshCommand = new Command(async () =>
                        await ExecuteCustomersRefreshCommand()));
            }
        }

        async Task ExecuteCustomersRefreshCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            _CustomersRefreshCommand.ChangeCanExecute();

            await FetchCustomers();

            IsBusy = false;
            _CustomersRefreshCommand.ChangeCanExecute();
        }

        Command _DialNumberCommand;

        /// <summary>
        /// Command to dial customer phone number
        /// </summary>
        public Command DialNumberCommand
        {
            get
            {
                return _DialNumberCommand ??
                    (_DialNumberCommand = new Command(async (parameter) =>
                        await ExecuteDialNumberCommand((string)parameter)));
            }
        }

        async Task ExecuteDialNumberCommand(string customerId)
        {
            if (String.IsNullOrWhiteSpace(customerId))
                return;

            var customer = _Accounts.SingleOrDefault(c => c.Id == customerId);

            if (customer == null)
                return;

            if (await Page.DisplayAlert(
                title: $"Would you like to call {customer.DisplayName}?",
                message: "",
                accept: "Call",
                cancel: "Cancel"))
            {
                var phoneCallTask = MessagingPlugin.PhoneDialer;
                if (phoneCallTask.CanMakePhoneCall)
                    phoneCallTask.MakePhoneCall(customer.Phone.SanitizePhoneNumber());
            }
        }

        void SubscribeToSaveCustomerMessages()
        {
            // This subscribes to the "SaveCustomer" message, and then inserts or updates the customer accordingly
            MessagingCenter.Subscribe<Customer>(this, "SaveCustomer", async (customer) =>
                {
                    IsBusy = true;

                    if (string.IsNullOrWhiteSpace(customer.Id))
                    {
                        customer.Id = Guid.NewGuid().ToString();
                        customer.PhotoUrl = "placeholderProfileImage";
                    }

                    await DataSource.SaveItem(customer);

                    await FetchCustomers();

                    IsBusy = false;
                });
        }

        void SubscribeToDeleteCustomerMessages()
        {
            // This subscribes to the "DeleteCustomer" message, and then deletes the customer accordingly
            MessagingCenter.Subscribe<Customer>(this, "DeleteCustomer", async (customer) =>
                {
                    IsBusy = true;

                    await DataSource.DeleteItem(customer.Id);

                    await FetchCustomers();

                    IsBusy = false;
                });
        }
    }
}

