/*
 * Created by SharpDevelop.
 * User: Reinier
 * Date: 24/05/2015
 * Time: 12:30 a. m.
 * 
 * To fix:
	
   To try:
   		-Serial port data transmision in main thread to reduce delay
   		-Native socket implementation or better
   
   To add:
   		- 
   		
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace ModbusMaster
{	
	public partial class MainForm : Form
	{
		SerialPort sp1 = null;
		ModbusMaster master = new ModbusMaster();
	    string cad = "";	
		
	    
	    Thread rx_thread = null;
		TcpListener tcp = null;
		
		bool server_on = false;		
		
		public MainForm()
		{			
			InitializeComponent();		
		}
		
		public void tx_char(SerialPort SBUF, string x)
		{
			if (SBUF == null ) return;			
			if ( SBUF.IsOpen == false )
				SBUF.Open();		
			
			byte[] mBuffer = Encoding.ASCII.GetBytes(x);
			SBUF.Write(mBuffer, 0, mBuffer.Length);	
		}		
				
		void MainFormLoad(object sender, EventArgs e)
		{
			string[] ports = SerialPort.GetPortNames(); 
			comboBox1.Items.Clear(); 			
			foreach(string port in ports) 
			{ 
				comboBox1.Items.Add(port); 
			} 
			if ( comboBox1.Items.Count > 0 )
			{
			    comboBox1.Text = comboBox1.Items[0].ToString();
			}
			else
			{
				conButton.Enabled = false;
			}
			
			comboBox2.Items.Add("1200");
			comboBox2.Items.Add("2400"); 
			comboBox2.Items.Add("4800"); 
			comboBox2.Items.Add("9600"); 			
			comboBox2.Items.Add("19200"); 
			comboBox2.Text = "2400"; 	
			comboBox3.Items.Add(Parity.Even.ToString()); 
			comboBox3.Items.Add(Parity.Mark.ToString()); 
			comboBox3.Items.Add(Parity.None.ToString()); 
			comboBox3.Items.Add(Parity.Odd.ToString()); 
			comboBox3.Items.Add(Parity.Space.ToString()); 
			comboBox3.Text = "None";
			
			
			/* find host addresses */
			groupBox4.Enabled = false;
			
			String strHostName = string.Empty;
			strHostName = Dns.GetHostName();
			
			IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
			IPAddress[] addr = ipEntry.AddressList;
						
			for (int i = 0; i < addr.Length; i++)
			{
				string ip = addr[i].ToString();
				if ( ip.Contains(":") == false )	// remove IPv6 address
				{
			    	comboBox5.Items.Add(addr[i].ToString());
				}
			}
			
			if ( comboBox5.Items.Count > 0 )
			{
				comboBox5.Text = comboBox5.Items[0].ToString();
			}
		}							
		
		private void port_DataReceived(object sender, SerialDataReceivedEventArgs e) 
		{ 			 					
			cad = sp1.ReadExisting();			
			this.Invoke(new EventHandler(Actualizar_RX));
		}
		
		void MainFormFormClosing(object sender, FormClosingEventArgs e)
		{
			if ( sp1 != null && sp1.IsOpen )
				sp1.Close();
			if (server_on == true)
			{
				StopServer();
			}
		}
		
		void ConButtonClick(object sender, EventArgs e)
		{
			if ( sp1 == null || sp1.IsOpen == false )
			{
				Parity Paridad = Parity.None; 
				switch (comboBox3.Text)
				{
					case "Even" : Paridad = Parity.Even; 
								  break;								  
					case "Mark" : Paridad = Parity.Mark; 
								  break;
					case "Odd"  : Paridad = Parity.Odd; 
								  break;
					case "Space": Paridad = Parity.Space; 					
								  break;																	  
					default		: Paridad = Parity.None;
								  break;
				}													
				sp1 = new SerialPort(comboBox1.Text,Convert.ToInt16(comboBox2.Text),Paridad,8,StopBits.One);
				sp1.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived); 
				sp1.Open();
				conButton.Text = "Desconectar";
			}
			else
			{				
				sp1.Close();
				conButton.Text = "Conectar";
			}
		}
		
		private void Actualizar_RX(object s, EventArgs e)
		{															
			this.textBox2.AppendText(cad);
		}
		
		private void Actualizar_TX(object s, EventArgs e)
		{															
			this.textBox6.AppendText(cad);
		}
		
		private void Actualizar_LOG(object s, EventArgs e)
		{												
			DateTime hour = DateTime.Now;
			string head = hour.Hour.ToString() + ":" + hour.Minute.ToString();
			this.textBox5.AppendText(head + " " + cad + "\n");
		}
		
		
		void Button3Click(object sender, EventArgs e)
		{
			textBox2.Clear();
		}
		
		void Button5Click(object sender, System.EventArgs e)
		{
			textBox6.Clear();
		}
		
		void Button6Click(object sender, EventArgs e)
		{
			int val = 0;
			if ( sp1 == null || sp1.IsOpen == false )
				return;		
						
			if ( button6.Text == "Apagar equipo" )
			{
				val = 0;
				button6.Text = "Encender equipo";				
			}
			else
			{
				val = 1;
				button6.Text = "Apagar equipo";				
			}
			
			try
			{
				uint dir = Convert.ToUInt16(textBox3.Text.ToString());
			
				int start = 1;
			
				
				master.ForceSingleCoil_05((int)dir,start,val);
				master.tx_ascii_frame(sp1);
				textBox6.AppendText(master.ascii_frame);
				
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}
		
		void Button7Click(object sender, EventArgs e)
		{	
			if ( sp1 == null || sp1.IsOpen == false )
				return;				
				 				
			try
			{
				uint dir = Convert.ToUInt16(textBox3.Text.ToString());				
				uint num = Convert.ToUInt16(textBox1.Text.ToString());
				if ( num < 0 || num > 19 )
				{
					MessageBox.Show("El número a mostrar debe estar entre 0 y 19." );
					return;
				}
					
				master.PresetSingleRegister_06((int)dir,1,(int)num);	//cambiar numero
				master.tx_ascii_frame(sp1);
				textBox6.AppendText(master.ascii_frame);
				
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}
		
		void Button10Click(object sender, EventArgs e)
		{
			MessageBox.Show("Aplicación creada por Reinier César Mujica Hernández - Profesor del Dpto. de Telecomunicaciones y Electrónica" +
							"     04/07/2016 " + "   email: remujica@uclv.cu  reinier.mujica@gmail.com.");
		}
		
		void Button1Click(object sender, EventArgs e)
		{
			int level = 0;
			
			if ( sp1 == null || sp1.IsOpen == false )
				return;				
				 				
			try
			{
				switch (comboBox4.Text)
				{
					case "Nivel 0" : level = 0; 
								  break;								  
					case "Nivel 1" : level = 1; 
								  break;
					case "Nivel 2" : level = 2; 
								  break;
					case "Nivel 3" : level = 3; 					
								  break;																	  
					case "Nivel 4" : level = 4; 
								  break;
					case "Nivel 5" : level = 5; 
								  break;
					default		: level = 3;
								  MessageBox.Show("El nivel de brillo no es correcto." );
								  comboBox4.Text = "Nivel 3";
								  return;							
				}			
				
				uint dir = Convert.ToUInt16(textBox3.Text.ToString());	
								
				master.PresetSingleRegister_06((int)dir,2,level);	//cambiar brillo
				master.tx_ascii_frame(sp1);
				textBox6.AppendText(master.ascii_frame);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
				
		}
		
		void CheckBox1CheckedChanged(object sender, EventArgs e)
		{
			if ( ((CheckBox)sender).Checked == true )
			{
				groupBox4.Enabled = true;
				groupBox5.Enabled = true;
			}
			else
			{
				groupBox4.Enabled = false;
				groupBox5.Enabled = false;
				if ( server_on == true )
				{
					StopServer();
				}
			}
		}
		
	
		//frame types
		//D_255_19
		//D_255_X	only uppercase letters
		//B_4
		
		public void Proccess_Frame(string frame, ref Socket resp)
		{
			int addres = 0;
			int number = 0;
			int bright = 0;
			byte[] response = System.Text.Encoding.ASCII.GetBytes("OK$");
			
			if (frame[0] == 'D' && frame[1] == '_')
			{
				int cur = 2; 	//skip underscore
				while (frame[cur] != '_' && cur <= 4)
				{
					addres *= 10;
					addres += int.Parse((frame[cur] - '0').ToString());
					cur++;
				}
				if (addres < 0 || addres > 255) return;
				if (frame[cur] != '_') return;
				
				cur++;
				if (frame[cur] >= 'A' && frame[cur] <= 'Z')		// is a letter
				{
					if (frame[cur+1] != '$') return;
					number = int.Parse((frame[cur] - 'A').ToString());	
				}
				else
				{
					if (frame[cur] < '0' || frame[cur] > '9') return;
					while (frame[cur] != '$' && cur <= 7)
					{
						number *= 10;
						number += int.Parse((frame[cur] - '0').ToString());
						cur++;
					}
					if (frame[cur] != '$') return;
					if (number > 19) return;
				}
				
				resp.Send(response,SocketFlags.None);	// response valid frame OK$
			
				cad = Encoding.ASCII.GetString(response);
				this.Invoke(new EventHandler(Actualizar_TX));
			
				master.PresetSingleRegister_06(addres,1,number);
				master.tx_ascii_frame(sp1);
				
				cad = master.ascii_frame;
				this.Invoke(new EventHandler(Actualizar_TX));
			}
			
			//B_5
			if (frame[0] == 'B' && frame[1] == '_')
			{
				if (frame[2] < '0' || frame[2] > '5' || frame[3] != '$' ) return; // is not a number
				
				bright = int.Parse((frame[2] - '0').ToString());
		
				resp.Send(response,SocketFlags.None);	// response valid frame OK$
				
				cad = Encoding.ASCII.GetString(response);
				this.Invoke(new EventHandler(Actualizar_TX));
				
				master.PresetSingleRegister_06(00,2,bright);
				master.tx_ascii_frame(sp1);
				
				cad = master.ascii_frame;
				this.Invoke(new EventHandler(Actualizar_TX));
			}
		}
		
		public void Start()
		{				
			while (true)
			{
				try
				{
					Socket pak = tcp.AcceptSocket();
					
					cad = "Client conected to server";
					this.Invoke(new EventHandler(Actualizar_LOG));
					
					pak.ReceiveTimeout = 2000;
					pak.SendTimeout = 2000;
					
					byte[] b = new byte[256];
					pak.Receive(b);
					
					cad = "Data received from client";
					this.Invoke(new EventHandler(Actualizar_LOG));
					
					string msg = Encoding.ASCII.GetString(b);
				
					cad = msg;
					this.Invoke(new EventHandler(Actualizar_RX));
				
					Proccess_Frame(msg, ref pak);
					
					pak.Shutdown(SocketShutdown.Both);
					pak.Close();
				}
				catch (SocketException se)
				{
					cad = "Comunication Error: " + se.SocketErrorCode.ToString();
					this.Invoke(new EventHandler(Actualizar_LOG));
					
				}
				catch (Exception e)
				{
					cad = "General Error: " + e.ToString();
					this.Invoke(new EventHandler(Actualizar_LOG));
				}
			}	
		}
		
		void StopServer()
		{
			rx_thread.Suspend();
			tcp.Stop();
			if (tcp.Server.Connected == true) 
			{
				tcp.Server.Shutdown(SocketShutdown.Both);
				tcp.Server.Close();
			}
			button2.Text = "Crear servidor";
			label10.Text = "Servidor offline";
			label10.ForeColor = Color.Red;
			cad = "Server disconected";
			this.Invoke(new EventHandler(Actualizar_LOG));
			server_on = false;
		}
		
		void Button2Click(object sender, EventArgs e)
		{
			if ( server_on == false )
			{
				try
				{
					IPAddress ip = IPAddress.Parse(comboBox5.Text);
					int port	 = int.Parse(textBox4.Text);
				
					tcp = new TcpListener(ip, port);
					
					tcp.Start();
						
					ThreadStart rx = new ThreadStart(this.Start);
	
					rx_thread = new Thread(rx);
					rx_thread.IsBackground = true;				
					rx_thread.Start();
					
					button2.Text = "Detener servidor";
					label10.Text = "Servidor online";
					label10.ForeColor = Color.Green;
					
					cad = "Server active on " + ip.ToString() + ":" + port.ToString();
					this.Invoke(new EventHandler(Actualizar_LOG));
					server_on = true;
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message);
					server_on = false;
				}
			}
			else
			{	
				StopServer();	
			}
		}
	}
	
}
