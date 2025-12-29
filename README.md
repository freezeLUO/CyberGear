# ⚙ CyberGear

This repo is a .Net implementation of CyberGear.

## 👋 How to use?

Using the builder pattern to build ``CanBus``.

```csharp
var builder = CanBus.CreateBuilder(SlotType.Usb, 1);
builder.Configure(opt =>
{
    // set can bitrate, which default value is Pcan1000
	opt.Bitrate = Bitrate.Pcan1000;
    // set masteridd which default value is 0
    opt.MasterId = 0;
    // add motor can id range
    opt.AddMotors(new uint[] { 2, 127 });
});
CanBus can = builder.Build();
```

Where are the motors?

```csharp
var motor0 = can.Motors[0];
var motor1 = can.Motors[1];
var motorFeedback = await motor0.SetMechanicalZeroAsync();
```

## 🎨 3D Models

This repository includes SOLIDWORKS 2022 3D models and STL files for the CyberGear motor:

+ **Location**: [Cybergear model(SOLIDWORKS 2022 & STL)/](Cybergear%20model(SOLIDWORKS%202022%20&%20STL)/)
+ **Files**:
  - `Cybergear.SLDASM` - Assembly file
  - `motor.SLDPRT` - Motor part
  - `rotor.SLDPRT` - Rotor part
  - STL files for 3D printing

These models can be used for mechanical design, integration planning, and 3D printing.

## 👉 Matters

+ Please use a PCAN compatible device
+ [PEAK official website driver](https://peak-system.com.cn/driver/)  
+ Download ``Peak.Can.Basic(4.8.0.830) `` through ``nurget``
+ Example: ``CyberGearPlayground``

## 📒 Referrence：  
+ [PEAK Official Documentation](https://docs.peak-system.com/API/PCAN-Basic.Net/html/52acafbe-cf02-f99b-ad12-0942060b0289.htm ) 

+ [Introductory video](https://www.bilibili.com/video/BV1wQ3Ce7EBw/)