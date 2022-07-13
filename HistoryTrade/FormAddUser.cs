using HistoryTrade.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HistoryTrade
{
    public partial class FormAddUser : Form
    {
        static string path = Directory.GetCurrentDirectory() + "/users/";
        public FormAddUser()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            User user = new User();
            user.ApiId = textBox1.Text;
            user.ApiHash = textBox2.Text;
            user.PhoneNumber = textBox3.Text;
            user.UserName = textBox4.Text;
            string json = JsonConvert.SerializeObject(user);
            File.WriteAllText(path + user.UserName, json);
            Close();
        }
    }
}
