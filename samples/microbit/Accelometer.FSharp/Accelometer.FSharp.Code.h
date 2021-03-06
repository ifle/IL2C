﻿#ifndef __MODULE_Accelometer_FSharp_Code__
#define __MODULE_Accelometer_FSharp_Code__

#include <il2c.h>
#include <stdbool.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

//////////////////////////////////////////////////////////////////////////////////
// Types:

typedef struct Accelometer_Demo Accelometer_Demo;

////////////////////////////////////////////////////////////
// Class: Accelometer.Demo

// Class vtable layout
typedef const struct
{
    /* internalcall */ void* (*IL2C_RuntimeCast)(Accelometer_Demo* this__, IL2C_RUNTIME_TYPE_DECL* type);
    System_String* (*ToString)(Accelometer_Demo* this__);
    int32_t (*GetHashCode)(Accelometer_Demo* this__);
    void (*Finalize)(Accelometer_Demo* this__);
    bool (*Equals)(Accelometer_Demo* this__, System_Object* obj);
} __Accelometer_Demo_VTABLE_DECL__;

// Class layout
struct Accelometer_Demo
{
    __Accelometer_Demo_VTABLE_DECL__* vptr0__;
};


// Class runtime type information
extern IL2C_RUNTIME_TYPE_DECL __Accelometer_Demo_RUNTIME_TYPE__;

////////////////////////////////////////////////////////////
// Enum: Accelometer.Demo.ArrowNames

// Enum layout
typedef enum Accelometer_Demo_ArrowNames
{
    Accelometer_Demo_ArrowNames_North = 0,
    Accelometer_Demo_ArrowNames_NorthEast = 1,
    Accelometer_Demo_ArrowNames_East = 2,
    Accelometer_Demo_ArrowNames_SouthEast = 3,
    Accelometer_Demo_ArrowNames_South = 4,
    Accelometer_Demo_ArrowNames_SouthWest = 5,
    Accelometer_Demo_ArrowNames_West = 6,
    Accelometer_Demo_ArrowNames_NorthWest = 7,
} Accelometer_Demo_ArrowNames;


// Enum runtime type information
extern IL2C_RUNTIME_TYPE_DECL __Accelometer_Demo_ArrowNames_RUNTIME_TYPE__;

////////////////////////////////////////////////////////////
// Enum: Accelometer.Demo.IconNames

// Enum layout
typedef enum Accelometer_Demo_IconNames
{
    Accelometer_Demo_IconNames_Heart = 0,
} Accelometer_Demo_IconNames;


// Enum runtime type information
extern IL2C_RUNTIME_TYPE_DECL __Accelometer_Demo_IconNames_RUNTIME_TYPE__;

//////////////////////////////////////////////////////////////////////////////////
// Public static fields:


//////////////////////////////////////////////////////////////////////////////////
// Methods:

// Accelometer.Demo
extern /* internalcall */ void __Accelometer_Demo_IL2C_MarkHandler__(Accelometer_Demo* this__);
extern /* internalcall */ void* __Accelometer_Demo_IL2C_RuntimeCast__(Accelometer_Demo* this__, IL2C_RUNTIME_TYPE_DECL* type);
extern void Accelometer_Demo_BeginAccelometer(bool highResolution, uint8_t scale);
extern uint8_t Accelometer_Demo_UpdateAccelometer(void);
extern int8_t Accelometer_Demo_GetAccelometerX(void);
extern int8_t Accelometer_Demo_GetAccelometerY(void);
extern int8_t Accelometer_Demo_GetAccelometerZ(void);
extern void Accelometer_Demo_BeginScreen(void);
extern void Accelometer_Demo_ShowArrow(Accelometer_Demo_ArrowNames direction, uint32_t interval);
extern void Accelometer_Demo_ShowIcon(Accelometer_Demo_IconNames icon, uint32_t interval);
extern void Accelometer_Demo_Delay(int32_t millis);
extern void Accelometer_Demo_Main(void);
#define Accelometer_Demo_ToString(/* Accelometer_Demo* */ this__) \
    ((this__)->vptr0__->ToString(this__))
#define Accelometer_Demo_GetHashCode(/* Accelometer_Demo* */ this__) \
    ((this__)->vptr0__->GetHashCode(this__))
#define Accelometer_Demo_Finalize(/* Accelometer_Demo* */ this__) \
    ((this__)->vptr0__->Finalize(this__))
#define Accelometer_Demo_Equals(/* Accelometer_Demo* */ this__, /* System_Object* */ obj) \
    ((this__)->vptr0__->Equals(this__, obj))

#ifdef __cplusplus
}
#endif

#endif
