/* add user code begin Header */
/**
  **************************************************************************
  * @file     main.c
  * @brief    main program
  **************************************************************************
  *                       Copyright notice & Disclaimer
  *
  * The software Board Support Package (BSP) that is made available to
  * download from Artery official website is the copyrighted work of Artery.
  * Artery authorizes customers to use, copy, and distribute the BSP
  * software and its related documentation for the purpose of design and
  * development in conjunction with Artery microcontrollers. Use of the
  * software is governed by this copyright notice and the following disclaimer.
  *
  * THIS SOFTWARE IS PROVIDED ON "AS IS" BASIS WITHOUT WARRANTIES,
  * GUARANTEES OR REPRESENTATIONS OF ANY KIND. ARTERY EXPRESSLY DISCLAIMS,
  * TO THE FULLEST EXTENT PERMITTED BY LAW, ALL EXPRESS, IMPLIED OR
  * STATUTORY OR OTHER WARRANTIES, GUARANTEES OR REPRESENTATIONS,
  * INCLUDING BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY,
  * FITNESS FOR A PARTICULAR PURPOSE, OR NON-INFRINGEMENT.
  *
  **************************************************************************
  */
/* add user code end Header */

/* Includes ------------------------------------------------------------------*/
#include "at32f403a_407_wk_config.h"

/* private includes ----------------------------------------------------------*/
/* add user code begin private includes */
#include "ads131m0x_function.h"
#include "DA16600.h"
/* add user code end private includes */

/* private typedef -----------------------------------------------------------*/
/* add user code begin private typedef */

/* add user code end private typedef */

/* private define ------------------------------------------------------------*/
/* add user code begin private define */

/* add user code end private define */

/* private macro -------------------------------------------------------------*/
/* add user code begin private macro */

/* add user code end private macro */

/* private variables ---------------------------------------------------------*/
/* add user code begin private variables */

/* add user code end private variables */

/* private function prototypes --------------------------------------------*/
/* add user code begin function prototypes */

/* add user code end function prototypes */

/* private user code ---------------------------------------------------------*/
/* add user code begin 0 */
adc_channel_data adc_data;
bool init_flag = FALSE, wait_cmd_flag = FALSE, wait_connect_flag = FALSE, wakeup_flag = FALSE, reset_flag = FALSE;
uint8_t buffer[TCP_LEN+20] = "S1961,0,0,", check_cnt = 0;
/* add user code end 0 */

/**
  * @brief main function.
  * @param  none
  * @retval none
  */
int main(void)
{
  /* add user code begin 1 */
	delay_init();
  /* add user code end 1 */

  /* system clock config. */
  wk_system_clock_config();

  /* config periph clock. */
  wk_periph_clock_config();

  /* init debug function. */
  wk_debug_config();

  /* nvic config. */
  wk_nvic_config();

  /* init spi1 function. */
  wk_spi1_init();

  /* init spi2 function. */
  wk_spi2_init();

  /* init exint function. */
  wk_exint_config();

  /* init tmr2 function. */
  wk_tmr2_init();

  /* init gpio function. */
  wk_gpio_config();

  /* add user code begin 2 */
	adcStartup();
	gpio_bits_write(GPIOA, GPIO_PINS_8, TRUE);
	gpio_bits_write(GPIOA, GPIO_PINS_9, TRUE);
	wait_connect_flag = TRUE;
  /* add user code end 2 */

  while(1)
  {
    /* add user code begin 3 */
		while(!wakeup_flag)
		{
			if(int_flag)
			{
				if(read_response())
				{
					ReadData();
				}
			}
			
			/*if(strncmp(re_data, "\r\n+WFCST", 8) == 0)
			{
				wakeup_flag = TRUE;
			}*/
			
			if(!reset_flag)
			{
				if(strncmp(re_data, "\r\n+WFCST", 8) == 0)
				{
					wakeup_flag = TRUE;
				}
			}
			else
			{
				if(strncmp(re_data, "\r\nOK\r\n", 6) == 0)
				{
					wakeup_flag = TRUE;
				}
			}
		}
		if(!init_flag && wakeup_flag)
		{
			//wait PC connect wifi
			/*while(wait_connect_flag)
			{
				if(int_flag)
				{
					wait_connect_flag = FALSE;
					if(read_response())
					{
						ReadData();
					}
				}
			}*/
			//create TCP Client
			Write_Request("AT+TRTC=10.0.0.2,1234,2300");
			wait_cmd_flag = TRUE;
			while(wait_cmd_flag)
			{
				if(int_flag)
				{
					wait_cmd_flag = FALSE;
					if(read_response())
					{
						ReadData();
					}
				}
			}
			//if create TCP Client fail retry
			if(strncmp(re_data, "\r\nERROR:-736\r\n", 14))
			{
				wait_cmd_flag = TRUE;
				while(wait_cmd_flag)
				{
					if(int_flag)
					{
						wait_cmd_flag = FALSE;
						if(read_response())
						{
							ReadData();
						}
					}
				}
				init_flag = TRUE;
			}
			
		}
		else
		{
			if(waitForDRDYinterrupt(100))
			{
				readData(&adc_data);
				extract_low_24_bits(&adc_data);
				if(adc_data_cnt >= TCP_LEN)
				{
					adc_data_cnt = 0;
					memcpy(buffer+10, adc_tx_data, sizeof(adc_tx_data));
					buffer[10+sizeof(adc_tx_data)] = check_cnt;
					check_cnt++;
					check_cnt &= 0xff;
					send_data(buffer);
				}
			}
			if(int_flag)
			{
				wait_cmd_flag = FALSE;
				if(read_response())
				{
					ReadData();
				}
			}
			if(strncmp(re_data, "\r\nOK\r\n", 6) == 0)
			{
				
			}
			else if(strncmp(re_data, "\r\n+WFDST", 8) == 0)
			{
				Write_Request("AT+TRTALL");
				wakeup_flag = FALSE;
				init_flag = FALSE;
				reset_flag = FALSE;
			}
			else if(strncmp(re_data, "\r\n+TRXTC", 8) == 0)
			{
				Write_Request("AT+TRTALL");
				wakeup_flag = FALSE;
				init_flag = FALSE;
				reset_flag = TRUE;
			}
			else
			{
				Write_Request("AT+TRTALL");
				wakeup_flag = FALSE;
				init_flag = FALSE;
				reset_flag = TRUE;
			}
		}
    /* add user code end 3 */
  }
}
