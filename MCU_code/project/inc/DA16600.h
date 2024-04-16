#include "at32f403a_407.h"
#include "stdbool.h"
#include "string.h"


extern bool int_flag;
extern uint8_t int_cnt;
extern uint8_t re_data[100];

uint8_t SPI_Write(uint8_t);
void Write_Request(uint8_t*);
void send_data(uint8_t*);
int read_response(void);
void ReadData(void);
