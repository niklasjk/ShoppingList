using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShoppingList
{
    public class ListItem
    {
        public string title { get; set; }
        public string path { get; set; } = "test"; 
        public int check { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public Android.Net.Uri uri { get; set; } = null;

        [Newtonsoft.Json.JsonIgnore]
        public Android.Graphics.Bitmap bm = null;
        
        public String toString()
        {
            return "Title:" + title + "\nPath:" + path;
        }
    }
}