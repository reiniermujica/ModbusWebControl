using System;
using System.IO.Ports;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Text;
using System.Threading;

namespace ModbusMaster
{	
	public class ModbusMaster
	{		
		public string ascii_frame = ":";		
		private byte CR = 13, LF = 10;				
		SerialPort TSBUF = null;
		private int delay = 25;	// 25 miliseconds delay between tx bytes
		
		public ModbusMaster()
		{
			ascii_frame = "";
		}	
			
		public void tx_ascii_frame(SerialPort SBUF)
		{		
			if (SBUF == null ) return;			
			if ( SBUF.IsOpen == false )
				SBUF.Open();	
			TSBUF = SBUF;						
			
			/* start background serial tx */
			ThreadStart tx_frame = new ThreadStart(tx_data);
        	Thread thread = new Thread(tx_frame);
        	thread.Start();			
		}			
					
		private void tx_data()
    	{
			byte[] mBuffer = null;
			
        	for (int i = 0; i < ascii_frame.Length; i++ )
			{        		
        		mBuffer = Encoding.ASCII.GetBytes(ascii_frame[i].ToString());
				TSBUF.Write(mBuffer, 0, mBuffer.Length);				
				Thread.Sleep(delay);				
			}
        	
        	mBuffer = new byte[2];
        	mBuffer[0] = CR; mBuffer[1] = LF;
        	
        	TSBUF.Write(mBuffer, 0, mBuffer.Length);
			Thread.Sleep(delay);        				
			
			TSBUF.Write(mBuffer,1,1);
			Thread.Sleep(delay);        			
	    }		
 		
		/* auxiliar functions */
		private void gen_start()
		{
    		ascii_frame = ":";
		}		
		private void gen_dir(int dir)
		{
			add_char(dir,2);
		}
		private void gen_function(int fun)
		{
			add_char(fun,2);
		}		
		private void gen_lrc()
		{
			add_char(lrc_calc(),2);
		}				
		private void add_char(int ch, int space)
		{
			string t = Convert.ToString(ch,16);
			string tmp = "";			
			for (int i = 0; i < t.Length; i++ )
			{
				if ( t[i] >= 'a' && t[i] <= 'z' )
				{
					tmp += Convert.ToChar(t[i] - 32);				
				}
				else
				{
					tmp += t[i];
				}
			}
			
			if (space == 2)
			{				
				if ( tmp.Length == 1 )
				{
					tmp = "0" + tmp;
				}								
			}
			if (space == 4)
			{
				switch (tmp.Length)
				{
					case 1 : tmp = "000" + tmp;
							 break;
					case 2 : tmp = "00" + tmp;
							 break;
					case 3 : tmp = "0" + tmp;
							 break;					
					default : break;
				}
			}		
			ascii_frame += tmp;
		}		
		byte AsciiToByte(char a, char b)
		{
			int _a = Convert.ToInt32(a);
			int _b = Convert.ToInt32(b);
		    if ( _a >= 65 )        
		    {
		        _a = _a - 65 + 10;
		    }
		    else
		    {
		        _a = _a - 48;    
		    }
		    
		    if ( _b >= 65 )
		    {
		        _b = _b - 65 + 10;
		    }
		    else
		    {
		        _b = _b - 48;    
		    }		       
		    return Convert.ToByte((16 * _a) + _b);
		}		
		
		public int lrc_calc() 
		{
		    int result = 0;    		  
		    for ( int i = 1; i < ascii_frame.Length; i+=2 )
		    {		    			    
		    	result += AsciiToByte(ascii_frame[i], ascii_frame[i+1]);		    	
		    }		    
		    result = result & 0xFF;
			result = 0xFF - result;		    
		    return result + 1;
		}
		
		private void clear_frame()
		{    
		    ascii_frame = "";
		}
		
		/* Modbus functions codes */
		public void ReadCoilStatus_01(int dir, int start, int cant) 
		{		    		
		    gen_start();
		    gen_dir(dir);
		    gen_function(1);   		   
		    add_char(start - 1,4);		    
		    add_char(cant,4);		    
		    gen_lrc();			
		}		
		public void ReadInputStatus_02(int dir, int start, int cant) 
		{		  
		    gen_start();
		    gen_dir(dir);
		    gen_function(2);   		    		    
		    add_char(start - 1, 4);		    
		    add_char(cant,4);		    
		    gen_lrc();		
		}		
		public void ReadHoldingRegisters_03(int dir, int start, int cant) 
		{		   		  
		    gen_start();
		    gen_dir(dir);
		    gen_function(3);    		    
		    add_char(start - 1, 4);		    
		    add_char(cant,4);		    
		    gen_lrc();				    
		}				
		public void ReadInputRegisters_04(int dir, int start, int cant)  
		{		   
		    gen_start();
		    gen_dir(dir);
		    gen_function(4);    		    
		  	add_char(start - 1, 4);		    
		    add_char(cant,4);		    
		    gen_lrc();			
		}		
		public void ForceSingleCoil_05(int dir, int coilID, int value) 
		{		   
		    gen_start();
		    gen_dir(dir);
		    gen_function(5);    		  
		    add_char(coilID - 1, 4);		    
		    if ( value > 0 )
		    {
		        add_char(65280, 4);  // value FF00
		    }
		    else
		    {
		    	add_char(0, 4);      // value 0000
		    }				    		
		    gen_lrc();				 
		}		
		public void PresetSingleRegister_06(int dir, int registerID, int value) 
		{		    
		    gen_start();
		    gen_dir(dir);
		    gen_function(6);   		    
		    add_char(registerID - 1, 4);		    
		    add_char(value, 4); 		   
		    gen_lrc();				    
		}		
		public void ForceMultipleCoils_15(int dir, int start, int cant, byte[] parameters)    
		{		  
		    gen_start();
		    gen_dir(dir);
		    gen_function(15);     		    		    
		    add_char(start - 1, 4);		    
		    add_char(cant, 4);		    
		    add_char(parameters.Length, 2);		
		    for ( int i = 0; i < parameters.Length; i++ )
		    {
		    	add_char(parameters[i], 2);		        
		    }		
		    gen_lrc();		
		}		
		
		public void PresetMultipleRegisters_16(int dir, int start, int cant, byte[] parameters)  
		{		 
		    gen_start();
		    gen_dir(dir);
		    gen_function(16);    	
		    add_char(start - 1, 4);		
		    add_char(cant, 4);		   		    
		    add_char(parameters.Length, 2);		
		    for ( int i = 0; i < parameters.Length; i++ )
		    {
		    	add_char(parameters[i], 2);		        
		    }		    
		    gen_lrc();				   
		}
	}	
}
