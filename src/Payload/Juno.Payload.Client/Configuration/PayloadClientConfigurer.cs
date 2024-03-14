//using System;
//using Microsoft.Localization.ApiClient;

//namespace Juno.Payload.Client.Configuration
//{
//    public class PayloadClientConfigurer : IApiClientConfigurer
//    {
//        private readonly Uri _baseUri;
//        private readonly Uri _msiScope;
//        internal PayloadClientConfigurer(string baseUri, string msiScope=null){

//            this._baseUri = new Uri(baseUri);
//            if (msiScope != null)
//            {
//                this._msiScope = new Uri(msiScope);
//            }

//        }

//        public void Configure(IApiClientConfigurationBuilder builder)
//        {
//            builder.AddBaseUrl(_baseUri.ToString());

//            if (this._msiScope != null)
//            {
//                builder.EnableMSI(new[] { this._msiScope });
//            }
//        }

//        public void ConfigureDefaultQueryString(IQueryStringDefinitionBuilder queryStringDefinitionBuilder)
//        {
//        }

//        public void ConfigureDefaultRoute(IRouteDefinitionBuilder routeDefinitionBuilder)
//        {
//        }
//    }
//}
