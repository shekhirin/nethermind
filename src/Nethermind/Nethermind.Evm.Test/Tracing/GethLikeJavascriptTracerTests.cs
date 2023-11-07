// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Nethermind.Core.Extensions;
using Nethermind.Evm.Tracing.GethStyle;
using NUnit.Framework;
using Nethermind.Specs;
using Nethermind.Core.Test.Builders;
using Nethermind.Evm.Tracing.GethStyle.Javascript;
using Nethermind.Int256;
using Nethermind.Specs.Forks;

namespace Nethermind.Evm.Test.Tracing;

public class GethLikeJavascriptTracerTests : VirtualMachineTestsBase
{
    [TestCase("{ result: function(ctx, db) { return null } }", TestName = "fault")]
    [TestCase("{ fault: function(log, db) { } }", TestName = "result")]
    [TestCase("{ fault: function(log, db) { }, result: function(ctx, db) { return null }, enter: function(frame) { } }", TestName = "exit")]
    [TestCase("{ fault: function(log, db) { }, result: function(ctx, db) { return null }, exit: function(frame) { } }", TestName = "enter")]
    public void missing_functions(string tracer)
    {
        Action trace = () => ExecuteBlock(GetTracer(tracer), MStore());
        trace.Should().Throw<ArgumentException>();
    }

    [Test]
    public void log_operations()
    {
        string userTracer = @"{
                    retVal: [],
                    step: function(log, db) { this.retVal.push(log.getPC() + ':' + log.op.toString() + ':' + log.getCost() + ':' + log.getGas() + ':' + log.getRefund()) },
                    fault: function(log, db) { this.retVal.push('FAULT: ' + JSON.stringify(log)) },
                    result: function(ctx, db) { return this.retVal }
                }";
        GethLikeTxTrace traces = ExecuteBlock(
                GetTracer(userTracer),
                MStore(),
                MainnetSpecProvider.CancunActivation)
            .BuildResult().First();
        string[] expectedStrings = { "0:PUSH32:0:79000:0", "33:PUSH1:0:78997:0", "35:MSTORE:0:78994:0", "36:PUSH32:0:78988:0", "69:PUSH1:0:78985:0", "71:MSTORE:0:78982:0", "72:STOP:0:78976:0" };
        traces.CustomTracerResult.Should().BeEquivalentTo(expectedStrings);
    }

    private GethLikeBlockJavascriptTracer GetTracer(string userTracer) => new(TestState, Shanghai.Instance, GethTraceOptions.Default with { EnableMemory = true, Tracer = userTracer });


    [Test]
    public void log_operation_functions()
    {
        string userTracer = @"{
                    retVal: [],
                    step: function(log, db) { this.retVal.push(log.op.toString() + ' : ' + log.op.toNumber() + ' : ' + log.op.isPush() ) },
                    fault: function(log, db) { this.retVal.push('FAULT: ' + JSON.stringify(log)) },
                    result: function(ctx, db) { return this.retVal }
                }";
        GethLikeTxTrace traces = ExecuteBlock(
                GetTracer(userTracer),
                MStore(),
                MainnetSpecProvider.CancunActivation)
            .BuildResult().First();
        string[] expectedStrings = { "PUSH32 : 127 : true", "PUSH1 : 96 : true", "MSTORE : 82 : false", "PUSH32 : 127 : true", "PUSH1 : 96 : true", "MSTORE : 82 : false", "STOP : 0 : false" };
        traces.CustomTracerResult.Should().BeEquivalentTo(expectedStrings);
    }

    [Test]
    public void log_stack_functions()
    {
        string userTracer = @"{
                    retVal: [],
                    step: function(log, db) { this.retVal.push(log.stack.length()) },
                    fault: function(log, db) { this.retVal.push('FAULT: ' + JSON.stringify(log)) },
                    result: function(ctx, db) { return this.retVal }
                }";
        GethLikeTxTrace traces = ExecuteBlock(
                GetTracer(userTracer),
                MStore(),
                MainnetSpecProvider.CancunActivation)
            .BuildResult().First();
        int[] expected = { 0, 1, 2, 0, 1, 2, 0 };
        traces.CustomTracerResult.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void log_memory_functions()
    {
        string userTracer = @"{
                    retVal: [],
                         step: function(log, db) {
                        if (log.op.toNumber() == 0x52) {
                            this.retVal.push(log.memory.length());
                        } else if (log.op.toNumber() == 0x00) {
                            this.retVal.push(log.memory.length());
                        }
                    },
                    fault: function(log, db) { this.retVal.push('FAULT: ' + JSON.stringify(log.getError())) },
                    result: function(ctx, db) { return this.retVal }
                }";
        GethLikeTxTrace traces = ExecuteBlock(
                GetTracer(userTracer),
                MStore(),
                MainnetSpecProvider.CancunActivation)
            .BuildResult().First();
        int[] expectedResult = { 0, 32, 64 };
        traces.CustomTracerResult.Should().BeEquivalentTo(expectedResult);
    }

    [Test]
    public void log_contract_functions()
    {
        string userTracer = @"{
                    retVal: [],
                    step: function(log, db) { this.retVal.push(log.contract.getAddress() + ':' + log.contract.getCaller() + ':' + log.contract.getInput()) },
                    fault: function(log, db) { this.retVal.push('FAULT: ' + JSON.stringify(log)) },
                    result: function(ctx, db) { return this.retVal }
                }";
        GethLikeTxTrace traces = ExecuteBlock(
                GetTracer(userTracer),
                MStore(),
                MainnetSpecProvider.CancunActivation)
            .BuildResult().First();
        string[] expectedStrings =
        {
            "148,41,33,177,79,27,28,56,92,215,224,204,46,247,171,229,89,140,131,88:183,112,90,228,198,248,27,102,205,179,35,198,95,78,129,51,105,15,192,153:",
            "148,41,33,177,79,27,28,56,92,215,224,204,46,247,171,229,89,140,131,88:183,112,90,228,198,248,27,102,205,179,35,198,95,78,129,51,105,15,192,153:",
            "148,41,33,177,79,27,28,56,92,215,224,204,46,247,171,229,89,140,131,88:183,112,90,228,198,248,27,102,205,179,35,198,95,78,129,51,105,15,192,153:",
            "148,41,33,177,79,27,28,56,92,215,224,204,46,247,171,229,89,140,131,88:183,112,90,228,198,248,27,102,205,179,35,198,95,78,129,51,105,15,192,153:",
            "148,41,33,177,79,27,28,56,92,215,224,204,46,247,171,229,89,140,131,88:183,112,90,228,198,248,27,102,205,179,35,198,95,78,129,51,105,15,192,153:",
            "148,41,33,177,79,27,28,56,92,215,224,204,46,247,171,229,89,140,131,88:183,112,90,228,198,248,27,102,205,179,35,198,95,78,129,51,105,15,192,153:",
            "148,41,33,177,79,27,28,56,92,215,224,204,46,247,171,229,89,140,131,88:183,112,90,228,198,248,27,102,205,179,35,198,95,78,129,51,105,15,192,153:"
        };
        traces.CustomTracerResult.Should().BeEquivalentTo(expectedStrings);
    }

    [Test]
    public void Js_traces_simple_filter()
    {
        string userTracer = "{" +
                            "retVal: []," +
                            "step: function(log, db) { this.retVal.push(log.getPC() + ':' + log.op.toString()) }," +
                            "fault: function(log, db) { this.retVal.push('FAULT: ' + JSON.stringify(log)) }," +
                            "result: function(ctx, db) { return this.retVal }" +
                            "}";
        ;

        GethLikeTxTrace traces = ExecuteBlock(
                GetTracer(userTracer),
                MStore(),
                MainnetSpecProvider.CancunActivation)
            .BuildResult().First();
        string[] expectedStrings = { "0:PUSH32", "33:PUSH1", "35:MSTORE", "36:PUSH32", "69:PUSH1", "71:MSTORE", "72:STOP" };
        Assert.That(traces.CustomTracerResult, Is.EqualTo(expectedStrings));
    }

    [Test]
    public void filter_with_conditionals()
    {
        string userTracer = @"{
                    retVal: [],
                    step: function(log, db) {
                        if (log.op.toNumber() == 0x60) {
                            this.retVal.push(log.getPC() + ': PUSH1');
                        } else if (log.op.toNumber() == 0x52) {
                            this.retVal.push(log.getPC() + ': MSTORE');
                        }
                    },
                    fault: function(log, db) { this.retVal.push('FAULT: ' + JSON.stringify(log)); },
                    result: function(ctx, db) { return this.retVal; }
                }";
        GethLikeTxTrace traces = ExecuteBlock(
                GetTracer(userTracer),
                MStore(),
                MainnetSpecProvider.CancunActivation)
            .BuildResult().First();
        string[] expectedStrings = { "33: PUSH1", "35: MSTORE", "69: PUSH1", "71: MSTORE" };
        Assert.That(traces.CustomTracerResult, Is.EqualTo(expectedStrings));
    }

    [Test]
    public void storage_information()
    {
        string userTracer = @"{
                    retVal: [],
                    step: function(log, db) {
                        if (log.op.toNumber() == 0x55)
                            this.retVal.push(log.getPC() + ': SSTORE ' + log.stack.peek(0).toString(16));
                        if (log.op.toNumber() == 0x54)
                            this.retVal.push(log.getPC() + ': SLOAD ' + log.stack.peek(0).toString(16));
                        if (log.op.toNumber() == 0x00)
                            this.retVal.push(log.getPC() + ': STOP ' + log.stack.peek(0).toString(16) + ' <- ' + log.stack.peek(1).toString(16));
                    },
                    fault: function(log, db) {
                        this.retVal.push('FAULT: ' + JSON.stringify(log));
                    },
                    result: function(ctx, db) {
                        return this.retVal;
                    }
                }";
        GethLikeTxTrace traces = ExecuteBlock(
                GetTracer(userTracer),
                SStore_double(),
                MainnetSpecProvider.CancunActivation)
            .BuildResult().First();
        string[] expectedStrings = { "35: SSTORE 0", "71: SSTORE 20", "107: SLOAD 0", "108: STOP a01234 <- a01234" };
        Assert.That(traces.CustomTracerResult, Is.EqualTo(expectedStrings));
    }

    [Test]
    public void operation_results()
    {
        string userTracer = """
                            {
                                retVal: [],
                                afterSload: false,
                                step: function(log, db) {
                                    if (this.afterSload) {
                                            this.retVal.push("Result: " + log.stack.peek(0).toString(16));
                                        this.afterSload = false;
                                    }
                                    if (log.op.toNumber() == 0x54) {
                                            this.retVal.push(log.getPC() + " SLOAD " + log.stack.peek(0).toString(16));
                                        this.afterSload = true;
                                    }
                                    if (log.op.toNumber() == 0x55)
                                        this.retVal.push(log.getPC() + " SSTORE " + log.stack.peek(0).toString(16) + " <- " + log.stack.peek(1).toString(16));
                                },
                                fault: function(log, db) {
                                    this.retVal.push("FAULT: " + JSON.stringify(log));
                                },
                                result: function(ctx, db) {
                                    return this.retVal;
                                }
                            }
                            """;
        GethLikeTxTrace traces = ExecuteBlock(
                GetTracer(userTracer),
                SStore(),
                MainnetSpecProvider.CancunActivation)
            .BuildResult().First();
        string[] expectedStrings = { "68 SSTORE 1 <- a01234", "104 SLOAD 1", "Result: a01234" };
        Assert.That(traces.CustomTracerResult, Is.EqualTo(expectedStrings));
    }

    [Test]
    public void calls_btn_contracts()
    {
        string userTracer = """
                            {
                                retVal: [],
                                afterSload: false,
                                callStack: [],
                                byte2Hex: function(byte) {
                                    if (byte < 0x10) {
                                        return "0" + byte.toString(16);
                                    }
                                    return byte.toString(16);
                                },
                                array2Hex: function(arr) {
                                    var retVal = "";
                                    for (var i=0; i<arr.length; i++) {
                                        retVal += this.byte2Hex(arr[i]);
                                    }
                                    return retVal;
                                },
                                getAddr: function(log) {
                                    return this.array2Hex(log.contract.getAddress());
                                },
                                step: function(log, db) {
                                    var opcode = log.op.toNumber();
                                    // SLOAD
                                    if (opcode == 0x54) {
                                        this.retVal.push(log.getPC() + ": SLOAD " +
                                            this.getAddr(log) + ":" +
                                            log.stack.peek(0).toString(16));
                                        this.afterSload = true;
                                    }
                                    // SLOAD Result
                                    if (this.afterSload) {
                                        this.retVal.push("Result: " +
                                            log.stack.peek(0).toString(16));
                                        this.afterSload = false;
                                    }
                                    // SSTORE
                                    if (opcode == 0x55) {
                                        this.retVal.push(log.getPC() + ": SSTORE " +
                                            this.getAddr(log) + ":" +
                                            log.stack.peek(0).toString(16) + " <- " +
                                            log.stack.peek(1).toString(16));
                                    }
                                    // End of step

                                },
                                fault: function(log, db) {
                                    this.retVal.push("FAULT: " + JSON.stringify(log));
                                },
                                result: function(ctx, db) {
                                    return this.retVal;
                            }
                        }
                        """;

        GethLikeTxTrace traces = ExecuteBlock(
                GetTracer(userTracer),
                SStore(),
                MainnetSpecProvider.CancunActivation)
            .BuildResult().First();
        string[] expectedStrings = { "68: SSTORE 942921b14f1b1c385cd7e0cc2ef7abe5598c8358:1 <- a01234", "104: SLOAD 942921b14f1b1c385cd7e0cc2ef7abe5598c8358:1", "Result: 1" };
        Assert.That(traces.CustomTracerResult, Is.EqualTo(expectedStrings));
    }

    [Test]
    public void noop_tracer_legacy()
    {
        GethLikeTxTrace traces = ExecuteBlock(
                GetTracer("noopTracer"),
                MStore(),
                MainnetSpecProvider.CancunActivation)
            .BuildResult().First();
        Assert.That(traces.CustomTracerResult, Has.All.Empty);
    }

    [Test]
    public void opcount_tracer()
    {
        GethLikeTxTrace traces = ExecuteBlock(
                GetTracer("opcountTracer"),
                MStore(),
                MainnetSpecProvider.CancunActivation)
            .BuildResult().First();
        Assert.That(traces.CustomTracerResult, Is.EqualTo(7));
    }

    [Test]
    public void prestate_tracer()
    {
        GethLikeTxTrace traces = ExecuteBlock(
                GetTracer("prestateTracer"),
                NestedCalls(),
                MainnetSpecProvider.CancunActivation)
            .BuildResult().First();

        Assert.That(JsonSerializer.Serialize(traces.CustomTracerResult), Is.EqualTo("{\"942921b14f1b1c385cd7e0cc2ef7abe5598c8358\":{\"balance\":\"0x56bc75e2d63100000\",\"nonce\":0,\"code\":\"60006000600060007376e68a8696537e4141926f3e528733af9e237d6961c350f400\",\"storage\":{}},\"76e68a8696537e4141926f3e528733af9e237d69\":{\"balance\":\"0xde0b6b3a7640000\",\"nonce\":0,\"code\":\"7f7f000000000000000000000000000000000000000000000000000000000000006000527f0060005260036000f30000000000000000000000000000000000000000000000602052602960006000f000\",\"storage\":{}},\"d75a3a95360e44a3874e691fb48d77855f127069\":{\"balance\":\"0x0\",\"nonce\":0,\"code\":\"\",\"storage\":{}},\"b7705ae4c6f81b66cdb323c65f4e8133690fc099\":{\"balance\":\"0x56bc75e2d63100000\",\"nonce\":0,\"code\":\"\",\"storage\":{}}}"));
    }

    private static byte[] MStore()
    {
        return Prepare.EvmCode
            .PushData(SampleHexData1.PadLeft(64, '0'))
            .PushData(0)
            .Op(Instruction.MSTORE)
            .PushData(SampleHexData2.PadLeft(64, '0'))
            .PushData(32)
            .Op(Instruction.MSTORE)
            .Op(Instruction.STOP)
            .Done;
    }

    private static byte[] SStore_double()
    {
        return Prepare.EvmCode
            .PushData(SampleHexData1.PadLeft(64, '0'))
            .PushData(0)
            .Op(Instruction.SSTORE)
            .PushData(SampleHexData2.PadLeft(64, '0'))
            .PushData(32)
            .Op(Instruction.SSTORE)
            .PushData(SampleHexData1.PadLeft(64, '0'))
            .PushData(0)
            .Op(Instruction.SLOAD)
            .Op(Instruction.STOP)
            .Done;
    }

    private static byte[] SStore()
    {
        return Prepare.EvmCode
            .PushData(SampleHexData2.PadLeft(64, '0'))
            .PushData(SampleHexData1.PadLeft(64, '0'))
            .PushData(UInt256.One)
            .Op(Instruction.SSTORE)
            .PushData(SampleHexData1.PadLeft(64, '0'))
            .PushData(UInt256.One)
            .Op(Instruction.SLOAD)
            .Op(Instruction.STOP)
            .Done;
    }

    private byte[] NestedCalls()
    {
        byte[] deployedCode = new byte[3];

        byte[] initCode = Prepare.EvmCode
            .ForInitOf(deployedCode)
            .Done;

        byte[] createCode = Prepare.EvmCode
            .Create(initCode, 0)
            .Op(Instruction.STOP)
            .Done;

        TestState.CreateAccount(TestItem.AddressC, 1.Ether());
        TestState.InsertCode(TestItem.AddressC, createCode, Spec);
        return Prepare.EvmCode
            .DelegateCall(TestItem.AddressC, 50000)
            .Op(Instruction.STOP)
            .Done;
    }
}