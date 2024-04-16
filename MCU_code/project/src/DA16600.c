#include "DA16600.h"
#include "ads131m0x.h"

uint8_t process_data[100]={0},re_data[100]={0},check[100]={0},int_cnt=0;
uint16_t lenth=0;
bool int_flag=FALSE;

uint8_t SPI_Write(uint8_t tx_data)
{
  uint8_t rx_data;
	while(spi_i2s_flag_get(SPI2, SPI_I2S_TDBE_FLAG) == RESET);
  gpio_bits_write(GPIOB, GPIO_PINS_12, FALSE);
  spi_i2s_data_transmit(SPI2, tx_data);
  while(spi_i2s_flag_get(SPI2, SPI_I2S_RDBF_FLAG) == RESET);
  rx_data=spi_i2s_data_receive(SPI2);
	
  gpio_bits_write(GPIOB, GPIO_PINS_12, TRUE);
	//tmr_counter_enable(TMR1, TRUE);
	return rx_data;
}

uint8_t SPI_Read(uint8_t tx_data)
{
  uint8_t rx_data;
  gpio_bits_write(GPIOB, GPIO_PINS_12, FALSE);
  while(spi_i2s_flag_get(SPI2, SPI_I2S_TDBE_FLAG) == RESET);
  spi_i2s_data_transmit(SPI2, tx_data);
  while(spi_i2s_flag_get(SPI2, SPI_I2S_RDBF_FLAG) == RESET);
  rx_data=spi_i2s_data_receive(SPI2);
  gpio_bits_write(GPIOB, GPIO_PINS_12, TRUE);

  return rx_data;
}

void Write_Request(uint8_t AT_CMD[])
{
	uint8_t length=strlen(AT_CMD);
	uint8_t remainder =(length & 0x3);//%4
	uint8_t len=length;
	if(remainder!=0)length=length+(4-remainder);
	SPI_Write(0x50);
	SPI_Write(0x08);
	SPI_Write(0x02);
	SPI_Write(0x60);
	
	SPI_Write(0x80);//WRite cmd
	
	//length
	SPI_Write(0x00);
	SPI_Write( (length & 0xFF00 ) >> 8);
	SPI_Write( length & 0XFF );
	
	for(int i=0;i<len;i++)
	{
		SPI_Write(AT_CMD[i]);
	}
	
	for (int i=0;i<4-remainder;i++)
	{

			SPI_Write(0x00);

	}
}

void send_data(uint8_t data[])
{
	uint16_t length=TCP_LEN+11; //strlen(data)
	uint16_t len=length;
	length++;
	uint8_t remainder =(length & 0x3);//%4
	
	if(remainder!=0)length=length+(4-remainder);
	SPI_Write(0x50);
	SPI_Write(0x08);
	SPI_Write(0x02);
	SPI_Write(0x60);
	
	SPI_Write(0x80);//WRite cmd
	
	//length
	SPI_Write(0x00);
	SPI_Write( (length & 0xFF00 ) >> 8);
	SPI_Write( length & 0XFF );
	
	//SPI_Write(0x00);
	SPI_Write(0x1b);
	for(int i=0;i<len;i++)
	{
		SPI_Write(data[i]);
	}
	
	if(remainder!=0)
	{
		for (int i=0;i<4-remainder;i++)
		{

			SPI_Write(0x00);

		}
	}
        
}

int read_response(void)
{
	int_flag=false;
	SPI_Write(0x50);
	SPI_Write(0x08);
	SPI_Write(0x02);
	SPI_Write(0x58);
	
	SPI_Write(0xC0);
	
	SPI_Write(0x00);
	SPI_Write(0x00);
	SPI_Write(0x08);
	
	process_data[0]=SPI_Write(0x00);//ADD4
	process_data[1]=SPI_Write(0x00);//ADD3
	process_data[2]=SPI_Write(0x00);//ADD2
	process_data[3]=SPI_Write(0x00);//ADD1
	process_data[4]=SPI_Write(0x00);//LENTH2
	process_data[5]=SPI_Write(0x00);//LENTH1
	process_data[6]=SPI_Write(0x00);//RESPONSE
	process_data[7]=SPI_Write(0x00);//PADDING
	if((process_data[0]==0xff)|((process_data[1]==0xff))|(process_data[2]==0xff)|(process_data[3]==0xff)|((process_data[6]!=0x20)&&(process_data[6]!=0x83))) //|(process_data[6]!=0x20)
	{
		return 0;
	}
	
	return 1;

}

void ReadData(void)
{
	//Write ADD
	SPI_Write(process_data[3]);
	SPI_Write(process_data[2]);
	SPI_Write(process_data[1]);
	SPI_Write(process_data[0]);
	
	//READ CMD
	SPI_Write(0xC0);
	
	//LENGTH
	SPI_Write(0x00);
	SPI_Write(process_data[5]);
	SPI_Write(process_data[4]);
	lenth=(process_data[5]<<8)|(process_data[4]);
	
	for(int i=0;i<lenth;i++)
	{
			re_data[i]=SPI_Write(0x00);
	}
}
