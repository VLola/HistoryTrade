using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Telegram.Bot;
using Telegram.Bot.Types.InputFiles;
using TL;

namespace HistoryTrade
{
    public partial class Form1 : Form
    {
		private readonly ManualResetEventSlim _codeReady = new ManualResetEventSlim();
		private WTelegram.Client _client;
		private User _user;
		private List<Symbol> symbols = new List<Symbol>();
		private List<FullHistory> full_history = new List<FullHistory>();
		private static TelegramBotClient Bot;
		private static string token { get; set; } = "5432994682:AAEEWPQ_3G2xyLLPaYVzf_JGw0-dcwb--wY";
		public Form1()
		{
			InitializeComponent();
			DataGridViewColumn column1 = new DataGridViewColumn();
			DataGridViewColumn column2 = new DataGridViewColumn();
			DataGridViewColumn column3 = new DataGridViewColumn();
			DataGridViewColumn column4 = new DataGridViewColumn();
			DataGridViewColumn column5 = new DataGridViewColumn();
			column1.Name = "Symbol";
			column1.HeaderText = "Symbol";
			column1.Width = 100;
			column1.CellTemplate = new DataGridViewTextBoxCell();
			dataGridView1.Columns.Add(column1);
			column2.Name = "Count +";
			column2.HeaderText = "Count +";
			column2.Width = 50;
			column2.CellTemplate = new DataGridViewTextBoxCell();
			dataGridView1.Columns.Add(column2);
			column3.Name = "Profit";
			column3.HeaderText = "Profit";
			column3.Width = 50;
			column3.CellTemplate = new DataGridViewTextBoxCell();
			dataGridView1.Columns.Add(column3);
			column4.Name = "Count -";
			column4.HeaderText = "Count -";
			column4.Width = 50;
			column4.CellTemplate = new DataGridViewTextBoxCell();
			dataGridView1.Columns.Add(column4);
			column5.Name = "Loss";
			column5.HeaderText = "Loss";
			column5.Width = 50;
			column5.CellTemplate = new DataGridViewTextBoxCell();
			dataGridView1.Columns.Add(column5);
			WTelegram.Helpers.Log = (l, s) => Debug.WriteLine(s);
			Bot = new TelegramBotClient(token);
		}
		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			_client?.Dispose();
			Properties.Settings.Default.Save();
		}
		private void linkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Process.Start(((LinkLabel)sender).Tag as string);
		}
		private async void buttonLogin_Click(object sender, EventArgs e)
		{
			buttonLogin.Enabled = false;
			listBox.Items.Add($"Connecting & login into Telegram servers...");
			_client = new WTelegram.Client(Config);
			_user = await _client.LoginUserIfNeeded();
			listBox.Items.Add($"We are now connected as {_user}");
			_client.Update += Client_Update;
		}
		string Config(string what)
		{
			switch (what)
			{
				case "api_id": return "10489970";
				case "api_hash": return "ccbd2e27673871e4f2fc32360b83b8d3";
				case "phone_number": return "+380982667643";
				case "verification_code":
				case "password":
					BeginInvoke(new Action(() => CodeNeeded(what.Replace('_', ' '))));
					_codeReady.Reset();
					_codeReady.Wait();
					return textBoxCode.Text;
				default: return null;
			};
		}
		private void CodeNeeded(string what)
		{
			labelCode.Text = what + ':';
			textBoxCode.Text = "";
			labelCode.Visible = textBoxCode.Visible = buttonSendCode.Visible = true;
			textBoxCode.Focus();
			listBox.Items.Add($"A {what} is required...");
		}
		private void buttonSendCode_Click(object sender, EventArgs e)
		{
			labelCode.Visible = textBoxCode.Visible = buttonSendCode.Visible = false;
			_codeReady.Set();
		}
		private void Client_Update(IObject arg)
		{
			UpdatesBase updates = (UpdatesBase)arg;
			if (updates == null) return;
            else
            {
				foreach (var update in updates.UpdateList)
                {
					switch (update)
                    {
						case UpdateNewMessage unm: DisplayMessage(unm.message); break;
					}
				}

			}
		}
		private void DisplayMessage(MessageBase messageBase)
		{
			if (messageBase.Peer.ID == 1729192251)
			{
				TL.Message m = (TL.Message)messageBase;
				if (m.message.Contains("#") && !m.message.Contains("Sold") && !m.message.Contains("Bought"))
				{
					symbols.Add(new Symbol(m.message));
					full_history.Clear();
					dataGridView1.Rows.Clear();

					foreach (var item in symbols)
					{
						if (full_history.Count > 0)
						{
							bool check = false;
							foreach (var it in full_history)
							{
								if (item.SymbolName == it.SymbolName)
								{
									check = true;
									if (item.isPositive)
									{
										it.Profit += item.Profit;
										it.CountPositive++;
									}
									else
									{
										it.Loss += item.Profit;
										it.CountNegative++;
									}
								}
							}
							if (!check)
							{
								FullHistory history = new FullHistory();
								history.SymbolName = item.SymbolName;
								if (item.isPositive)
								{
									history.Profit = item.Profit;
									history.CountPositive++;
								}
								else
								{
									history.Loss = item.Profit;
									history.CountNegative++;
								}
								full_history.Add(history);
							}
						}
						else
						{
							FullHistory history = new FullHistory();
							history.SymbolName = item.SymbolName;
							if (item.isPositive)
							{
								history.Profit = item.Profit;
								history.CountPositive++;
							}
							else
							{
								history.Loss = item.Profit;
								history.CountNegative++;
							}
							full_history.Add(history);
						}
					}
					foreach (var it in full_history)
					{
						dataGridView1.Rows.Add(it.SymbolName, it.CountPositive, it.Profit, it.CountNegative, it.Loss);
					}
					dataGridView1.AutoSize = true;
					string path_screen = Directory.GetCurrentDirectory() + @"/screen.jpg";
					Bitmap bmp = new Bitmap(this.dataGridView1.Width - 30, this.dataGridView1.Height - 30);
					this.dataGridView1.DrawToBitmap(bmp, this.dataGridView1.ClientRectangle);
					bmp.Save(path_screen);

					Telegram.Bot.Types.ChatId chatId = new Telegram.Bot.Types.ChatId(724154385);
					FileStream fsSource = new FileStream(path_screen, FileMode.Open, FileAccess.Read);
					InputOnlineFile file = new InputOnlineFile(fsSource);
					Bot.SendPhotoAsync(chatId: chatId, photo: file);
				}
			}
			//MessageBox.Show($"{messageBase.From.ID}");
			
		}
		private string TableString(string text)
		{
			while (text.Length < 20)
			{
				text += " ";
			}
			return text;
		}
		public class FullHistory
        {
			public string SymbolName { get; set; }
			public decimal Profit { get; set; }
			public decimal Loss { get; set; }
			public int CountPositive { get; set; }
			public int CountNegative { get; set; }
		}
		public class Symbol
        {
			public string SymbolName { get; set; }
			public decimal Profit { get; set; }
			public bool isPositive { get; set; }
			public Symbol(string text)
            {
				// Name
				int start = text.IndexOf('#');
				int end = text.IndexOf(',');
				string name = text.Remove(end);
				SymbolName = name.Substring(++start);
				// Profit
				start = text.IndexOf('$');
				end = text.IndexOf('#');
				name = text.Remove(end);
				string price = name.Substring(++start);
				end = price.IndexOf('(');
				name = price.Remove(--end);
				Profit = Convert.ToDecimal(name.Replace('.', ','));
				if (Profit > 0m) isPositive = true;
				else isPositive = false;
			}
        }
	}
}
