type Ref;
type ContractName;
const unique null: Ref;
const unique A: ContractName;
function ConstantToRef(x: int) returns (ret: Ref);
function {:bvbuiltin "mod"} modBpl(x: int, y: int) returns (ret: int);
function keccak256(x: int) returns (ret: int);
function abiEncodePacked1(x: int) returns (ret: int);
function _SumMapping_OverSight(x: [Ref]int) returns (ret: int);
function abiEncodePacked2(x: int, y: int) returns (ret: int);
function abiEncodePacked1R(x: Ref) returns (ret: int);
function abiEncodePacked2R(x: Ref, y: int) returns (ret: int);
var Balance: [Ref]int;
var DType: [Ref]ContractName;
var Alloc: [Ref]bool;
var balance_ADDR: [Ref]int;
var Length: [Ref]int;
procedure {:inline 1} FreshRefGenerator() returns (newRef: Ref);
implementation FreshRefGenerator() returns (newRef: Ref)
{
havoc newRef;
assume ((Alloc[newRef]) == (false));
Alloc[newRef] := true;
assume ((newRef) != (null));
}

procedure {:inline 1} HavocAllocMany();
implementation HavocAllocMany()
{
var oldAlloc: [Ref]bool;
oldAlloc := Alloc;
havoc Alloc;
assume (forall  __i__0_0:Ref ::  ((oldAlloc[__i__0_0]) ==> (Alloc[__i__0_0])));
}

procedure boogie_si_record_sol2Bpl_int(x: int);
procedure boogie_si_record_sol2Bpl_ref(x: Ref);
procedure boogie_si_record_sol2Bpl_bool(x: bool);

axiom(forall  __i__0_0:int, __i__0_1:int :: {ConstantToRef(__i__0_0), ConstantToRef(__i__0_1)} (((__i__0_0) == (__i__0_1)) || ((ConstantToRef(__i__0_0)) != (ConstantToRef(__i__0_1)))));

axiom(forall  __i__0_0:int, __i__0_1:int :: {keccak256(__i__0_0), keccak256(__i__0_1)} (((__i__0_0) == (__i__0_1)) || ((keccak256(__i__0_0)) != (keccak256(__i__0_1)))));

axiom(forall  __i__0_0:int, __i__0_1:int :: {abiEncodePacked1(__i__0_0), abiEncodePacked1(__i__0_1)} (((__i__0_0) == (__i__0_1)) || ((abiEncodePacked1(__i__0_0)) != (abiEncodePacked1(__i__0_1)))));

axiom(forall  __i__0_0:[Ref]int ::  ((exists __i__0_1:Ref ::  ((__i__0_0[__i__0_1]) != (0))) || ((_SumMapping_OverSight(__i__0_0)) == (0))));

axiom(forall  __i__0_0:[Ref]int, __i__0_1:Ref, __i__0_2:int ::  ((_SumMapping_OverSight(__i__0_0[__i__0_1 := __i__0_2])) == (((_SumMapping_OverSight(__i__0_0)) - (__i__0_0[__i__0_1])) + (__i__0_2))));

axiom(forall  __i__0_0:int, __i__0_1:int, __i__1_0:int, __i__1_1:int :: {abiEncodePacked2(__i__0_0, __i__1_0), abiEncodePacked2(__i__0_1, __i__1_1)} ((((__i__0_0) == (__i__0_1)) && ((__i__1_0) == (__i__1_1))) || ((abiEncodePacked2(__i__0_0, __i__1_0)) != (abiEncodePacked2(__i__0_1, __i__1_1)))));

axiom(forall  __i__0_0:Ref, __i__0_1:Ref :: {abiEncodePacked1R(__i__0_0), abiEncodePacked1R(__i__0_1)} (((__i__0_0) == (__i__0_1)) || ((abiEncodePacked1R(__i__0_0)) != (abiEncodePacked1R(__i__0_1)))));

axiom(forall  __i__0_0:Ref, __i__0_1:Ref, __i__1_0:int, __i__1_1:int :: {abiEncodePacked2R(__i__0_0, __i__1_0), abiEncodePacked2R(__i__0_1, __i__1_1)} ((((__i__0_0) == (__i__0_1)) && ((__i__1_0) == (__i__1_1))) || ((abiEncodePacked2R(__i__0_0, __i__1_0)) != (abiEncodePacked2R(__i__0_1, __i__1_1)))));
var y_A: [Ref]int;
procedure {:inline 1} helper_A(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int) returns (__ret_0_: int);
implementation helper_A(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int) returns (__ret_0_: int)
{
call  {:cexpr "_OverSightFirstArg"} boogie_si_record_sol2Bpl_bool(false);
call  {:cexpr "this"} boogie_si_record_sol2Bpl_ref(this);
call  {:cexpr "msg.sender"} boogie_si_record_sol2Bpl_ref(msgsender_MSG);
call  {:cexpr "msg.value"} boogie_si_record_sol2Bpl_int(msgvalue_MSG);
call  {:cexpr "_OverSightLastArg"} boogie_si_record_sol2Bpl_bool(true);
assert {:first} {:sourceFile "C:\Users\shane\Desktop\verisol\Test\regressions\StateVarCall.sol"} {:sourceLine 5} (true);
assert {:first} {:sourceFile "C:\Users\shane\Desktop\verisol\Test\regressions\StateVarCall.sol"} {:sourceLine 5} (true);
__ret_0_ := 1;
return;
}

procedure {:inline 1} A_A_NoBaseCtor(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, a_s42: Ref);
implementation A_A_NoBaseCtor(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, a_s42: Ref)
{
var z_s41: int;
var __var_1: int;
// start of initialization
assume ((msgsender_MSG) != (null));
y_A[this] := 0;
// end of initialization
call  {:cexpr "_OverSightFirstArg"} boogie_si_record_sol2Bpl_bool(false);
call  {:cexpr "this"} boogie_si_record_sol2Bpl_ref(this);
call  {:cexpr "msg.sender"} boogie_si_record_sol2Bpl_ref(msgsender_MSG);
call  {:cexpr "msg.value"} boogie_si_record_sol2Bpl_int(msgvalue_MSG);
call  {:cexpr "a"} boogie_si_record_sol2Bpl_ref(a_s42);
call  {:cexpr "_OverSightLastArg"} boogie_si_record_sol2Bpl_bool(true);
assert {:first} {:sourceFile "C:\Users\shane\Desktop\verisol\Test\regressions\StateVarCall.sol"} {:sourceLine 6} (true);
assert {:first} {:sourceFile "C:\Users\shane\Desktop\verisol\Test\regressions\StateVarCall.sol"} {:sourceLine 7} (true);
havoc z_s41;
assert {:first} {:sourceFile "C:\Users\shane\Desktop\verisol\Test\regressions\StateVarCall.sol"} {:sourceLine 8} (true);
assume ((y_A[this]) >= (0));
call __var_1 := helper_A(this, msgsender_MSG, msgvalue_MSG);
y_A[this] := __var_1;
assume ((__var_1) >= (0));
call  {:cexpr "y"} boogie_si_record_sol2Bpl_int(y_A[this]);
assert {:first} {:sourceFile "C:\Users\shane\Desktop\verisol\Test\regressions\StateVarCall.sol"} {:sourceLine 9} (true);
assume ((y_A[this]) >= (0));
assert ((y_A[this]) == (1));
assert {:first} {:sourceFile "C:\Users\shane\Desktop\verisol\Test\regressions\StateVarCall.sol"} {:sourceLine 10} (true);
assume ((z_s41) >= (0));
call z_s41 := helper_A(this, msgsender_MSG, msgvalue_MSG);
z_s41 := z_s41;
call  {:cexpr "z"} boogie_si_record_sol2Bpl_int(z_s41);
assert {:first} {:sourceFile "C:\Users\shane\Desktop\verisol\Test\regressions\StateVarCall.sol"} {:sourceLine 11} (true);
assume ((z_s41) >= (0));
assert ((z_s41) == (1));
}

procedure {:constructor} {:public} {:inline 1} A_A(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, a_s42: Ref);
implementation A_A(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, a_s42: Ref)
{
var z_s41: int;
var __var_1: int;
call  {:cexpr "_OverSightFirstArg"} boogie_si_record_sol2Bpl_bool(false);
call  {:cexpr "this"} boogie_si_record_sol2Bpl_ref(this);
call  {:cexpr "msg.sender"} boogie_si_record_sol2Bpl_ref(msgsender_MSG);
call  {:cexpr "msg.value"} boogie_si_record_sol2Bpl_int(msgvalue_MSG);
call  {:cexpr "a"} boogie_si_record_sol2Bpl_ref(a_s42);
call  {:cexpr "_OverSightLastArg"} boogie_si_record_sol2Bpl_bool(true);
call A_A_NoBaseCtor(this, msgsender_MSG, msgvalue_MSG, a_s42);
}

procedure BoogieEntry_A();
implementation BoogieEntry_A()
{
var this: Ref;
var msgsender_MSG: Ref;
var msgvalue_MSG: int;
var choice: int;
var a_s42: Ref;
assume ((DType[this]) == (A));
call A_A(this, msgsender_MSG, msgvalue_MSG, a_s42);
while (true)
{
havoc msgsender_MSG;
havoc msgvalue_MSG;
havoc choice;
havoc a_s42;
}
}

procedure CorralChoice_A(this: Ref);
implementation CorralChoice_A(this: Ref)
{
var msgsender_MSG: Ref;
var msgvalue_MSG: int;
var choice: int;
var a_s42: Ref;
havoc msgsender_MSG;
havoc msgvalue_MSG;
havoc choice;
havoc a_s42;
}

procedure CorralEntry_A();
implementation CorralEntry_A()
{
var this: Ref;
var msgsender_MSG: Ref;
var msgvalue_MSG: int;
var a_s42: Ref;
assume ((DType[this]) == (A));
call A_A(this, msgsender_MSG, msgvalue_MSG, a_s42);
while (true)
{
call CorralChoice_A(this);
}
}


