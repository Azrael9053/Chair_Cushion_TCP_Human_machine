#include "at32f403a_407.h"
#include "ads131m0x.h"

extern bool flag_nDRDY_INTERRUPT;

void delay_init(void);
void delay_us(uint32_t);
void delay_ms(uint16_t);
void    InitADC(void);
void    spiSendReceiveArrays(const uint8_t DataTx[], uint8_t DataRx[], const uint8_t byteLength);
uint8_t spiSendReceiveByte(const uint8_t dataTx);
void    toggleRESET(void);
bool    waitForDRDYinterrupt(const uint32_t timeout_ms);
