using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace ThoughtStuff.ProductCatalogComposeExtension.Models
{
    public class Products
    {
        private static Products instance = null;
        private static readonly object Padlock = new object();
        public List<Product> ProductCatalog = new List<Product>();

        Products()
        {
            PopulateProducts();
        }

        public static Products Instance
        {
            get
            {
                lock (Padlock)
                {
                    if (instance == null)
                    {
                        instance = new Products();
                    }
                    return instance;
                }
            }
        }

        private void PopulateProducts()
        {
            Random random = new Random();
            string html = string.Empty;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://names.drycodes.com/500");
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                html = reader.ReadToEnd();
            }
            List<string> listItems = JsonConvert.DeserializeObject<List<string>>(html);
            foreach (var item in listItems)
            {
                var entry = new Product()
                {
                    Name = item,
                    SKU = random.Next(1, 5000).ToString(),
                    NumberInStock = random.Next(0, 50)
                };
                ProductCatalog.Add(entry);
            }

        }

    }



    public class Product
    {
        public string Name { get; set; }
        public string SKU { get; set; }
        public int NumberInStock { get; set; }
    }


}