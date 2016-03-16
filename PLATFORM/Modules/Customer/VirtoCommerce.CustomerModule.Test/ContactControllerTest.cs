﻿using System;
using System.Linq;
using System.Web.Http.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtoCommerce.CustomerModule.Data.Repositories;
using VirtoCommerce.CustomerModule.Data.Services;
using VirtoCommerce.CustomerModule.Web.Controllers.Api;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Customer.Model;
using VirtoCommerce.Platform.Data.DynamicProperties;
using VirtoCommerce.Platform.Data.Infrastructure.Interceptors;
using VirtoCommerce.Platform.Data.Repositories;


namespace VirtoCommerce.CustomerModule.Test
{
    [TestClass]
    public class ContactControllerTest
    {
        [TestMethod]
        public void SearchContactsTest()
        {
            var controller = GetContactController();
            var result = controller.Search(new SearchCriteria()) as OkNegotiatedContentResult<SearchResult>;
            Assert.IsNotNull(result.Content);
        }

        [TestMethod]
        public void CreateNewOrganization()
        {
            var controller = GetContactController();
            var org = new Organization
            {
                Id = "org2",
                BusinessCategory = "cat2",
                Name = "organization 2",
                ParentId = "org1"


            };
            var result = controller.CreateOrganization(org) as OkNegotiatedContentResult<Organization>;
            Assert.IsNotNull(result.Content);
        }

        [TestMethod]
        public void SearchTest()
        {
            var controller = GetContactController();
            var result = controller.Search(new SearchCriteria { MemberId = "org1" }) as OkNegotiatedContentResult<SearchResult>;
        }

        [TestMethod]
        public void GetContact()
        {
            var controller = GetContactController();
            var result = controller.GetContactById("testContact1") as OkNegotiatedContentResult<Contact>;
        }

        [TestMethod]
        public void CreateNewContact()
        {
            var controller = GetContactController();
            var contact = new Contact
            {
                Id = "testContact1",
                FullName = "Vasa2",
                BirthDate = DateTime.UtcNow,
                Organizations = new[] { "org1" },
                Addresses = new Address[]
                {
                    new Address {
                    Name = "some name",
                    AddressType = AddressType.Shipping,
                    City = "london",
                    Phone = "+68787687",
                    PostalCode = "22222",
                    CountryCode = "ENG",
                    CountryName = "England",
                    Email = "user@mail.com",
                    FirstName = "first name",
                    LastName = "last name",
                    Line1 = "line 1",
                    Organization = "org1"
                    }
                }.ToList(),
                Notes = new Note[] { new Note { Title = "1111", Body = "dfsdfs sdf sdf sdf sd" } },
                Emails = new[] { "uuu@mail.ru", "ssss@mail.ru" },
                Phones = new[] { "2322232", "32323232" },
                //DynamicPropertyValues = new[] { new DynamicPropertyObjectValue { Property = new DynamicProperty { Name = "testProp", ValueType = DynamicPropertyValueType.ShortText }, Values = new object[] { "sss" } } }.ToList(),
                DefaultLanguage = "ru"
            };
            var result = controller.CreateContact(contact) as OkNegotiatedContentResult<Contact>;
            Assert.IsNotNull(result.Content);
        }

        [TestMethod]
        public void UpdateContact()
        {
            var controller = GetContactController();
            var result = controller.GetContactById("testContact") as OkNegotiatedContentResult<Contact>;
            var contact = result.Content;

            contact.FullName = "diff name";
            contact.Emails.Remove(contact.Emails.FirstOrDefault());
            //contact.DynamicPropertyValues.Add(new DynamicPropertyObjectValue { Property = new DynamicProperty { Name = "setting2", ValueType = DynamicPropertyValueType.Integer }, Values = new object[] { "1223" } });

            controller.UpdateContact(contact);

            result = controller.GetContactById("testContact") as OkNegotiatedContentResult<Contact>;

            contact = result.Content;
        }

        [TestMethod]
        public void PartialUpdateContact()
        {
            var controller = GetContactController();
            var contact = new Contact
            {
                Id = "testContact",
                FullName = "ET"
            };

            controller.UpdateContact(contact);

            var result = controller.GetContactById("testContact") as OkNegotiatedContentResult<Contact>;

            contact = result.Content;
        }

        [TestMethod]
        public void DeleteContact()
        {
            var controller = GetContactController();
            controller.DeleteContacts(new[] { "testContact" });
            var result = controller.GetContactById("testStore") as OkNegotiatedContentResult<Contact>;

            Assert.IsNull(result);
        }


        private static CustomerModuleController GetContactController()
        {
            Func<IPlatformRepository> platformRepositoryFactory = () => new PlatformRepository("VirtoCommerce", new EntityPrimaryKeyGeneratorInterceptor(), new AuditableInterceptor(null));
            Func<ICustomerRepository> customerRepositoryFactory = () => new CustomerRepositoryImpl("VirtoCommerce", new EntityPrimaryKeyGeneratorInterceptor(), new AuditableInterceptor(null));

            var dynamicPropertyService = new DynamicPropertyService(platformRepositoryFactory);
            var memberService = new MemberServiceImpl(customerRepositoryFactory, dynamicPropertyService, null);
      
            return new CustomerModuleController(memberService, null);
        }
    }
}
