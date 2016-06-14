using System;
using Cosmos.IL2CPU.X86;
using CPU = Cosmos.Assembler.x86;
using Cosmos.Assembler.x86;
using XSharp.Compiler;
using Label = Cosmos.Assembler.Label;

namespace Cosmos.IL2CPU.X86.IL
{
    [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Beq)]
    [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Bge)]
    [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Bgt)]
    [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Ble)]
    [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Blt)]
    [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Bne_Un)]
    [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Bge_Un)]
    [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Bgt_Un)]
    [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Ble_Un)]
    [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Blt_Un)]
    [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Brfalse)]
    [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Brtrue)]
    public class Branch : ILOp
    {

        public Branch(Cosmos.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(MethodInfo aMethod, ILOpCode aOpCode)
        {
            var xIsSingleCompare = true;
            switch (aOpCode.OpCode)
            {
                case ILOpCode.Code.Beq:
                case ILOpCode.Code.Bge:
                case ILOpCode.Code.Bgt:
                case ILOpCode.Code.Bge_Un:
                case ILOpCode.Code.Bgt_Un:
                case ILOpCode.Code.Ble:
                case ILOpCode.Code.Ble_Un:
                case ILOpCode.Code.Bne_Un:
                case ILOpCode.Code.Blt:
                case ILOpCode.Code.Blt_Un:
                    xIsSingleCompare = false;
                    break;
            }

            var xStackContent = aOpCode.StackPopTypes[0];
            var xStackContentSize = SizeOfType(xStackContent);

            if (xStackContentSize > 8)
            {
                throw new Exception("Cosmos.IL2CPU.x86->IL->Branch.cs->Error: StackSize > 8 not supported");
            }

            CPU.ConditionalTestEnum xTestOp;
            // all conditions are inverted here?
            switch (aOpCode.OpCode)
            {
                case ILOpCode.Code.Beq:
                    xTestOp = CPU.ConditionalTestEnum.Zero;
                    break;
                case ILOpCode.Code.Bge:
                    xTestOp = CPU.ConditionalTestEnum.GreaterThanOrEqualTo;
                    break;
                case ILOpCode.Code.Bgt:
                    xTestOp = CPU.ConditionalTestEnum.GreaterThan;
                    break;
                case ILOpCode.Code.Ble:
                    xTestOp = CPU.ConditionalTestEnum.LessThanOrEqualTo;
                    break;
                case ILOpCode.Code.Blt:
                    xTestOp = CPU.ConditionalTestEnum.LessThan;
                    break;
                case ILOpCode.Code.Bne_Un:
                    xTestOp = CPU.ConditionalTestEnum.NotEqual;
                    break;
                case ILOpCode.Code.Bge_Un:
                    xTestOp = CPU.ConditionalTestEnum.AboveOrEqual;
                    break;
                case ILOpCode.Code.Bgt_Un:
                    xTestOp = CPU.ConditionalTestEnum.Above;
                    break;
                case ILOpCode.Code.Ble_Un:
                    xTestOp = CPU.ConditionalTestEnum.BelowOrEqual;
                    break;
                case ILOpCode.Code.Blt_Un:
                    xTestOp = CPU.ConditionalTestEnum.Below;
                    break;
                case ILOpCode.Code.Brfalse:
                    xTestOp = CPU.ConditionalTestEnum.Zero;
                    break;
                case ILOpCode.Code.Brtrue:
                    xTestOp = CPU.ConditionalTestEnum.NotZero;
                    break;
                default:
                    throw new Exception("Cosmos.IL2CPU.x86->IL->Branch.cs->Error: Unknown OpCode for conditional branch.");
            }
            if (!xIsSingleCompare)
            {
                if (xStackContentSize <= 4)
                {
                    //if (xStackContent.IsFloat)
                    //{
                    //    throw new Exception("Cosmos.IL2CPU.x86->IL->Branch.cs->Error: Comparison of floats (System.Single) is not yet supported!");
                    //}
                    //else
                    //{
                    new CPU.Pop {DestinationReg = CPU.RegistersEnum.EAX};
                    new CPU.Pop {DestinationReg = CPU.RegistersEnum.EBX};
                    XS.Compare(XSRegisters.OldToNewRegister(RegistersEnum.EBX), XSRegisters.OldToNewRegister(RegistersEnum.EAX));
                    new ConditionalJump {Condition = xTestOp, DestinationLabel = AppAssembler.TmpBranchLabel(aMethod, aOpCode)};
                    //}
                }
                else
                {
                    //if (xStackContent.IsFloat)
                    //{
                    //    throw new Exception("Cosmos.IL2CPU.x86->IL->Branch.cs->Error: Comparison of doubles (System.Double) is not yet supported!");
                    //}
                    //else
                    //{
                    var xNoJump = GetLabel(aMethod, aOpCode) + "__NoBranch";

                    // value 2  EBX:EAX
                    new CPU.Pop {DestinationReg = RegistersEnum.EAX};
                    new CPU.Pop {DestinationReg = RegistersEnum.EBX};
                    // value 1  EDX:ECX
                    new CPU.Pop {DestinationReg = RegistersEnum.ECX};
                    new CPU.Pop {DestinationReg = RegistersEnum.EDX};
                    switch (xTestOp)
                    {
                        case ConditionalTestEnum.Zero: // Equal
                        case ConditionalTestEnum.NotEqual: // NotZero
                            new CPU.Xor {DestinationReg = RegistersEnum.EAX, SourceReg = RegistersEnum.ECX};
                            new ConditionalJump {Condition = xTestOp, DestinationLabel = AppAssembler.TmpBranchLabel(aMethod, aOpCode)};
                            new CPU.Xor {DestinationReg = RegistersEnum.EBX, SourceReg = RegistersEnum.EDX};
                            new ConditionalJump {Condition = xTestOp, DestinationLabel = AppAssembler.TmpBranchLabel(aMethod, aOpCode)};
                            break;
                        case ConditionalTestEnum.GreaterThanOrEqualTo:
                            XS.Compare(XSRegisters.OldToNewRegister(RegistersEnum.EDX), XSRegisters.OldToNewRegister(RegistersEnum.EBX));
                            new ConditionalJump {Condition = ConditionalTestEnum.LessThan, DestinationLabel = xNoJump};
                            new ConditionalJump {Condition = ConditionalTestEnum.GreaterThan, DestinationLabel = AppAssembler.TmpBranchLabel(aMethod, aOpCode)};
                            XS.Compare(XSRegisters.OldToNewRegister(RegistersEnum.ECX), XSRegisters.OldToNewRegister(RegistersEnum.EAX));
                            new ConditionalJump {Condition = ConditionalTestEnum.Below, DestinationLabel = xNoJump};
                            break;
                        case ConditionalTestEnum.GreaterThan:
                            XS.Compare(XSRegisters.OldToNewRegister(RegistersEnum.EDX), XSRegisters.OldToNewRegister(RegistersEnum.EBX));
                            new ConditionalJump {Condition = ConditionalTestEnum.LessThan, DestinationLabel = xNoJump};
                            new ConditionalJump {Condition = ConditionalTestEnum.GreaterThan, DestinationLabel = AppAssembler.TmpBranchLabel(aMethod, aOpCode)};
                            XS.Compare(XSRegisters.OldToNewRegister(RegistersEnum.ECX), XSRegisters.OldToNewRegister(RegistersEnum.EAX));
                            new ConditionalJump {Condition = ConditionalTestEnum.BelowOrEqual, DestinationLabel = xNoJump};
                            break;
                        case ConditionalTestEnum.LessThanOrEqualTo:
                            XS.Compare(XSRegisters.OldToNewRegister(RegistersEnum.EDX), XSRegisters.OldToNewRegister(RegistersEnum.EBX));
                            new ConditionalJump {Condition = ConditionalTestEnum.LessThan, DestinationLabel = AppAssembler.TmpBranchLabel(aMethod, aOpCode)};
                            new ConditionalJump {Condition = ConditionalTestEnum.GreaterThan, DestinationLabel = xNoJump};
                            XS.Compare(XSRegisters.OldToNewRegister(RegistersEnum.ECX), XSRegisters.OldToNewRegister(RegistersEnum.EAX));
                            new ConditionalJump {Condition = ConditionalTestEnum.BelowOrEqual, DestinationLabel = AppAssembler.TmpBranchLabel(aMethod, aOpCode)};
                            break;
                        case ConditionalTestEnum.LessThan:
                            XS.Compare(XSRegisters.OldToNewRegister(RegistersEnum.EDX), XSRegisters.OldToNewRegister(RegistersEnum.EBX));
                            new ConditionalJump {Condition = ConditionalTestEnum.LessThan, DestinationLabel = AppAssembler.TmpBranchLabel(aMethod, aOpCode)};
                            new ConditionalJump {Condition = ConditionalTestEnum.GreaterThan, DestinationLabel = xNoJump};
                            XS.Compare(XSRegisters.OldToNewRegister(RegistersEnum.ECX), XSRegisters.OldToNewRegister(RegistersEnum.EAX));
                            new ConditionalJump {Condition = ConditionalTestEnum.Below, DestinationLabel = AppAssembler.TmpBranchLabel(aMethod, aOpCode)};
                            break;
                        // from here all unsigned
                        case ConditionalTestEnum.AboveOrEqual:
                            XS.Compare(XSRegisters.OldToNewRegister(RegistersEnum.EDX), XSRegisters.OldToNewRegister(RegistersEnum.EBX));
                            new ConditionalJump {Condition = ConditionalTestEnum.Above, DestinationLabel = AppAssembler.TmpBranchLabel(aMethod, aOpCode)};
                            XS.Compare(XSRegisters.OldToNewRegister(RegistersEnum.ECX), XSRegisters.OldToNewRegister(RegistersEnum.EAX));
                            new ConditionalJump {Condition = ConditionalTestEnum.Below, DestinationLabel = xNoJump};
                            break;
                        case ConditionalTestEnum.Above:
                            XS.Compare(XSRegisters.OldToNewRegister(RegistersEnum.EDX), XSRegisters.OldToNewRegister(RegistersEnum.EBX));
                            new ConditionalJump {Condition = ConditionalTestEnum.Above, DestinationLabel = AppAssembler.TmpBranchLabel(aMethod, aOpCode)};
                            XS.Compare(XSRegisters.OldToNewRegister(RegistersEnum.ECX), XSRegisters.OldToNewRegister(RegistersEnum.EAX));
                            new ConditionalJump {Condition = ConditionalTestEnum.BelowOrEqual, DestinationLabel = xNoJump};
                            break;
                        case ConditionalTestEnum.BelowOrEqual:
                            XS.Compare(XSRegisters.OldToNewRegister(RegistersEnum.EDX), XSRegisters.OldToNewRegister(RegistersEnum.EBX));
                            new ConditionalJump {Condition = ConditionalTestEnum.Above, DestinationLabel = AppAssembler.TmpBranchLabel(aMethod, aOpCode)};
                            new ConditionalJump {Condition = ConditionalTestEnum.Below, DestinationLabel = xNoJump};
                            XS.Compare(XSRegisters.OldToNewRegister(RegistersEnum.ECX), XSRegisters.OldToNewRegister(RegistersEnum.EAX));
                            new ConditionalJump {Condition = ConditionalTestEnum.Above, DestinationLabel = AppAssembler.TmpBranchLabel(aMethod, aOpCode)};
                            break;
                        case ConditionalTestEnum.Below:
                            XS.Compare(XSRegisters.OldToNewRegister(RegistersEnum.EDX), XSRegisters.OldToNewRegister(RegistersEnum.EBX));
                            new ConditionalJump {Condition = ConditionalTestEnum.Above, DestinationLabel = AppAssembler.TmpBranchLabel(aMethod, aOpCode)};
                            new ConditionalJump {Condition = ConditionalTestEnum.Below, DestinationLabel = xNoJump};
                            XS.Compare(XSRegisters.OldToNewRegister(RegistersEnum.ECX), XSRegisters.OldToNewRegister(RegistersEnum.EAX));
                            new CPU.ConditionalJump {Condition = CPU.ConditionalTestEnum.AboveOrEqual, DestinationLabel = AppAssembler.TmpBranchLabel(aMethod, aOpCode)};
                            break;
                        default:
                            throw new Exception("Unknown OpCode for conditional branch in 64-bit.");
                    }
                    new Label(xNoJump);
                    //}
                }
            }
            else
            {
                //if (xStackContent.IsFloat)
                //{
                //    throw new Exception("Cosmos.IL2CPU.x86->IL->Branch.cs->Error: Simple comparison of floating point numbers is not yet supported!");
                //}
                //else
                //{
                // todo: improve code clarity
                if (xStackContentSize <= 4)
                {
                    new CPU.Pop {DestinationReg = CPU.RegistersEnum.EAX};
                    if (xTestOp == ConditionalTestEnum.Zero)
                    {
                        new CPU.Compare {DestinationReg = CPU.RegistersEnum.EAX, SourceValue = 0};
                        new CPU.ConditionalJump {Condition = ConditionalTestEnum.Equal, DestinationLabel = AppAssembler.TmpBranchLabel(aMethod, aOpCode)};
                    }
                    else if (xTestOp == ConditionalTestEnum.NotZero)
                    {
                        new CPU.Compare {DestinationReg = CPU.RegistersEnum.EAX, SourceValue = 0};
                        new CPU.ConditionalJump {Condition = ConditionalTestEnum.NotEqual, DestinationLabel = AppAssembler.TmpBranchLabel(aMethod, aOpCode)};
                    }
                    else
                    {
                        throw new NotSupportedException("Cosmos.IL2CPU.x86->IL->Branch.cs->Error: Situation not supported yet! (In the Simple Comparison)");
                    }
                }
                else
                {
                    new CPU.Pop {DestinationReg = CPU.RegistersEnum.EAX};
                    new CPU.Pop {DestinationReg = CPU.RegistersEnum.EBX};
                    switch (xTestOp)
                    {
                        case ConditionalTestEnum.Zero: // Equal
                        case ConditionalTestEnum.NotZero: // NotEqual
                            new CPU.Xor { DestinationReg = CPU.RegistersEnum.EAX, SourceValue = 0 };
                            new CPU.ConditionalJump { Condition = xTestOp, DestinationLabel = AppAssembler.TmpBranchLabel(aMethod, aOpCode) };
                            new CPU.Xor { DestinationReg = CPU.RegistersEnum.EBX, SourceValue = 0 };
                            new CPU.ConditionalJump { Condition = xTestOp, DestinationLabel = AppAssembler.TmpBranchLabel(aMethod, aOpCode) };
                            break;
                        default:
                            throw new NotImplementedException("Cosmos.IL2CPU.X86.IL.Branch: Simple branch " + aOpCode.OpCode + " not implemented for operand ");
                    }
                }
            }
            //}
        }
    }
}
