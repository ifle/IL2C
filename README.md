# IL2C - A translator implementation of .NET intermediate language to C language.

## What's this?

* IL2C is a translator implementation of .NET intermediate language to C language, and the project for step-by-step designing and video recording.

* IL2C's implementation priorities:
  * Better predictability for runtime cost.
  * Better code portability.

* Public YouTube recorded videos: ["Making archive IL2C play list"](http://bit.ly/2xtu4MH)
  * "How to create IL2C or same tool of IL2CPP? This video contains pure design process through concept and details."

## Very simple translation sample

Original C# source code:

```csharp
namespace il2c_test_target
{
    public class Hoge1
    {
        public static int Add1(int a, bool isTwo)
        {
            return a + (isTwo ? 2 : 1);
        }
    }
}
```

Translate to C by [Milestone 3 version](https://github.com/kekyo/IL2C/commit/d94582a330fbb1aa0d62e73bc78344559abce3c2):

```c
int32_t il2c_test_target_Hoge1_Add1(int32_t a, bool isTwo)
{
    int32_t local0;

    int32_t __stack0_0;
    int32_t __stack1_0;

    __stack0_0 = a;
    __stack1_0 = isTwo ? 1 : 0;
    if (__stack1_0 != 0) goto L_0000;
    __stack1_0 = 1;
    goto L_0001;
L_0000:
    __stack1_0 = 2;
L_0001:
    __stack0_0 = __stack0_0 + __stack1_0;
    local0 = __stack0_0;
    goto L_0002;
L_0002:
    __stack0_0 = local0;
    return __stack0_0;
}
```

## License

Under Apache v2.

## Related informations

* Center CLR video letter: "Tested IL2C on micro:bit and arduino platform #8"
  * [![Tested IL2C on micro:bit and arduino platform #8](images/microbit.jpg)](http://bit.ly/2xGFo9J)

* Public conference session video: "Making archive IL2C #6-28 at .NET Conf 2017 Tokyo"
  * [.NET Conf 2017 in Tokyo Room B (In japanese)](https://csugjp.connpass.com/event/66004/)
  * [YouTube video](http://bit.ly/2hI1jTb)

* Milestone 2+ informations included the book of "Extensive Xamarin - Techbook Fest part 3"
  * [Extensive Xamarin - Xamaritans (In japanese)](http://bit.ly/2ycNVzW)

## Overall status

TODO: Writing...

### IL translation features

#### Support IL opcodes

| Issue | Status | Milestone
|:---|:---|:---|
| add | Partial supported | M1
| ldc family | Partial supported | M1
| ldarg family | Partial supported | M2
| ldloc family | Partial supported | M2
| stloc family | Partial supported | M2
| ldfld family | Partial supported | M2
| stfld family | Partial supported | M3
| call family | Partial supported | M3
| ret | Supported | M1
| initobj | Supported | M3
| newobj | Partial supported |

TODO:

#### Support primitive types

| Issue | Status | Milestone
|:---|:---|:---|
| System.Byte | Supported | M2
| System.SByte | Supported | M2
| System.Int16 | Supported | M2
| System.UInt16 | Supported | M2
| System.Int32 | Supported | M1
| System.UInt32
| System.Int64 | Supported | M1
| System.UInt64
| System.Single
| System.Double
| System.Boolean | Supported | M2
| System.Void | Partial supported | M2
| System.IntPtr
| System.UIntPtr

#### Support basic types

| Issue | Status | Milestone
|:---|:---|:---|
| System.Object | Partial supported |
| System.ValueType |
| System.String | Partial supported |
| System.Decimal |
| System.Guid |
| Value types | Partial supported | M3
| Class types | Partial supported |
| Managed reference | Partial supported | M3
| Unmanaged pointer |
| Enum types |
| Delegate types |
| Interface types |
| Nested types |
| Class inherits | Partial supported |
| Interface implements |
| Array types |
| Multi dimensional array types |
| Closed generic types |
| Open generic types | (not support) | -

#### Support type members

| Issue | Status | Milestone
|:---|:---|:---|
| Static field | Partial supported | M3
| Instance field | Partial supported | M3
| Static method | Partial supported | M3
| Instance method | Partial supported | M3
| Virtual method |
| Interop method (extern static) | Partial supported |
| Internal method (internal call) |
| Constructor | Partial supported |
| Constructor (ValueType) | Partial supported | M3
| Type initializer |
| Static property |
| Instance property |
| Static event |
| Instance event |

#### Support metadata handling

| Issue | Status | Milestone
|:---|:---|:---|
| Value type scopes | Partial supported | M3
| Class type scopes | Partial supported |
| Enum type scopes |
| Delegate type scopes |
| Interface type scopes |
| Nested type scopes |
| Field scopes | Partial supported | M3
| Method scopes | Partial supported | M3
| Property scopes |
| Event scopes |
| Can handle reflection | (not support) | -

#### Support runtime relates

| Issue | Status | Milestone
|:---|:---|:---|
| Referenced instance handler | Partial supported | 
| Value type boxed instance handler | Partial supported |
| Collect unused instance (GC) | Partial supported |
| Heap compaction (GC) |
| Monitor lock features |
| Platform invoke (P/Invoke) | Partial supported |
| Can handle unsafe pointers |
| Can handle marshaling features (Marshal) |

#### Application lifecycle managements

| Issue | Status | Milestone
|:---|:---|:---|
| Basic commandline driver | Partial supported | M2
| MSBuild (old/Task handler) |
| MSBuild (new/.NET Core CLI) |
| Platform independent core library |
| Host platform on .NET 4.5 | Supported | M1
| Host platform on .NET Core 1 | (not support) | -
| Host platform on .NET Core 2 | Supported |
| Host platform on mono |
| Support C99 compilers | Supported | M1
| Support non C99 (ANSI) compilers |
| Support better C code output via C++ symbol declarations |
| Support continuous integrations |

## Took pictures on design process

* #6-6: Near milestones

  ![#6-6: Near milestones](images/IMG_20170917_194810.jpg)

* #6-14: Data flow analysis

  ![#6-14: Data flow analysis](images/IMG_20170926_225355.jpg)

* Milestone 1: Tested on VC++ and we checked how C compiler's optimizer work.

  ![Milestone 1: Tested on VC++ and we checked how C compiler's optimizer work](images/il2c1.png)

* #6-48: How works mark-and-sweep garbage collection on translated code.

  ![#6-48: How works mark-and-sweep garbage collection on translated code](images/IMG_20171130_200549.jpg)
