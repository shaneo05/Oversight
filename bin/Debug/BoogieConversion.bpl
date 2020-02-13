type Ref;
type ContractName;
const unique null: Ref;
const unique A: ContractName;
const unique B: ContractName;
const unique C: ContractName;
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
var x_A: [Ref]int;
procedure {:inline 1} A_A_NoBaseCtor(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, a_s19: int);
implementation A_A_NoBaseCtor(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, a_s19: int)
{
// start of initialization
assume ((msgsender_MSG) != (null));
x_A[this] := 0;
// end of initialization
call  {:cexpr "_OverSightFirstArg"} boogie_si_record_sol2Bpl_bool(false);
call  {:cexpr "this"} boogie_si_record_sol2Bpl_ref(this);
call  {:cexpr "msg.sender"} boogie_si_record_sol2Bpl_ref(msgsender_MSG);
call  {:cexpr "msg.value"} boogie_si_record_sol2Bpl_int(msgvalue_MSG);
call  {:cexpr "a"} boogie_si_record_sol2Bpl_int(a_s19);
call  {:cexpr "_OverSightLastArg"} boogie_si_record_sol2Bpl_bool(true);
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\ConstructorChaining_fail.sol"} {:sourceLine 5} (true);
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\ConstructorChaining_fail.sol"} {:sourceLine 5} (true);
assume ((x_A[this]) >= (0));
assume ((a_s19) >= (0));
x_A[this] := a_s19;
call  {:cexpr "x"} boogie_si_record_sol2Bpl_int(x_A[this]);
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\ConstructorChaining_fail.sol"} {:sourceLine 5} (true);
assume ((x_A[this]) >= (0));
assume ((a_s19) >= (0));
assert ((x_A[this]) != (a_s19));
}

procedure {:constructor} {:public} {:inline 1} A_A(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, a_s19: int);
implementation A_A(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, a_s19: int)
{
call  {:cexpr "_OverSightFirstArg"} boogie_si_record_sol2Bpl_bool(false);
call  {:cexpr "this"} boogie_si_record_sol2Bpl_ref(this);
call  {:cexpr "msg.sender"} boogie_si_record_sol2Bpl_ref(msgsender_MSG);
call  {:cexpr "msg.value"} boogie_si_record_sol2Bpl_int(msgvalue_MSG);
call  {:cexpr "a"} boogie_si_record_sol2Bpl_int(a_s19);
call  {:cexpr "_OverSightLastArg"} boogie_si_record_sol2Bpl_bool(true);
call A_A_NoBaseCtor(this, msgsender_MSG, msgvalue_MSG, a_s19);
}

procedure {:inline 1} B_B_NoBaseCtor(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, a_s42: int);
implementation B_B_NoBaseCtor(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, a_s42: int)
{
// start of initialization
assume ((msgsender_MSG) != (null));
// end of initialization
call  {:cexpr "_OverSightFirstArg"} boogie_si_record_sol2Bpl_bool(false);
call  {:cexpr "this"} boogie_si_record_sol2Bpl_ref(this);
call  {:cexpr "msg.sender"} boogie_si_record_sol2Bpl_ref(msgsender_MSG);
call  {:cexpr "msg.value"} boogie_si_record_sol2Bpl_int(msgvalue_MSG);
call  {:cexpr "a"} boogie_si_record_sol2Bpl_int(a_s42);
call  {:cexpr "_OverSightLastArg"} boogie_si_record_sol2Bpl_bool(true);
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\ConstructorChaining_fail.sol"} {:sourceLine 9} (true);
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\ConstructorChaining_fail.sol"} {:sourceLine 9} (true);
assume ((x_A[this]) >= (0));
x_A[this] := (x_A[this]) + (1);
call  {:cexpr "x"} boogie_si_record_sol2Bpl_int(x_A[this]);
assume ((x_A[this]) >= (0));
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\ConstructorChaining_fail.sol"} {:sourceLine 9} (true);
assume ((x_A[this]) >= (0));
assume ((a_s42) >= (0));
assume (((a_s42) + (2)) >= (0));
assert ((x_A[this]) != ((a_s42) + (2)));
}

procedure {:constructor} {:public} {:inline 1} B_B(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, a_s42: int);
implementation B_B(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, a_s42: int)
{
call  {:cexpr "_OverSightFirstArg"} boogie_si_record_sol2Bpl_bool(false);
call  {:cexpr "this"} boogie_si_record_sol2Bpl_ref(this);
call  {:cexpr "msg.sender"} boogie_si_record_sol2Bpl_ref(msgsender_MSG);
call  {:cexpr "msg.value"} boogie_si_record_sol2Bpl_int(msgvalue_MSG);
call  {:cexpr "a"} boogie_si_record_sol2Bpl_int(a_s42);
call  {:cexpr "_OverSightLastArg"} boogie_si_record_sol2Bpl_bool(true);
assume ((a_s42) >= (0));
call A_A(this, msgsender_MSG, msgvalue_MSG, a_s42);
call B_B_NoBaseCtor(this, msgsender_MSG, msgvalue_MSG, a_s42);
}

procedure {:inline 1} C_C_NoBaseCtor(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, a_s65: int);
implementation C_C_NoBaseCtor(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, a_s65: int)
{
// start of initialization
assume ((msgsender_MSG) != (null));
// end of initialization
call  {:cexpr "_OverSightFirstArg"} boogie_si_record_sol2Bpl_bool(false);
call  {:cexpr "this"} boogie_si_record_sol2Bpl_ref(this);
call  {:cexpr "msg.sender"} boogie_si_record_sol2Bpl_ref(msgsender_MSG);
call  {:cexpr "msg.value"} boogie_si_record_sol2Bpl_int(msgvalue_MSG);
call  {:cexpr "a"} boogie_si_record_sol2Bpl_int(a_s65);
call  {:cexpr "_OverSightLastArg"} boogie_si_record_sol2Bpl_bool(true);
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\ConstructorChaining_fail.sol"} {:sourceLine 13} (true);
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\ConstructorChaining_fail.sol"} {:sourceLine 13} (true);
assume ((x_A[this]) >= (0));
x_A[this] := (x_A[this]) + (1);
call  {:cexpr "x"} boogie_si_record_sol2Bpl_int(x_A[this]);
assume ((x_A[this]) >= (0));
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\ConstructorChaining_fail.sol"} {:sourceLine 13} (true);
assume ((x_A[this]) >= (0));
assume ((a_s65) >= (0));
assume (((a_s65) + (2)) >= (0));
assert ((x_A[this]) != ((a_s65) + (2)));
}

procedure {:constructor} {:public} {:inline 1} C_C(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, a_s65: int);
implementation C_C(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, a_s65: int)
{
call  {:cexpr "_OverSightFirstArg"} boogie_si_record_sol2Bpl_bool(false);
call  {:cexpr "this"} boogie_si_record_sol2Bpl_ref(this);
call  {:cexpr "msg.sender"} boogie_si_record_sol2Bpl_ref(msgsender_MSG);
call  {:cexpr "msg.value"} boogie_si_record_sol2Bpl_int(msgvalue_MSG);
call  {:cexpr "a"} boogie_si_record_sol2Bpl_int(a_s65);
call  {:cexpr "_OverSightLastArg"} boogie_si_record_sol2Bpl_bool(true);
assume ((a_s65) >= (0));
call B_B(this, msgsender_MSG, msgvalue_MSG, a_s65);
call C_C_NoBaseCtor(this, msgsender_MSG, msgvalue_MSG, a_s65);
}

procedure BoogieEntry_A();
implementation BoogieEntry_A()
{
var this: Ref;
var msgsender_MSG: Ref;
var msgvalue_MSG: int;
var choice: int;
var a_s19: int;
assume ((((DType[this]) == (A)) || ((DType[this]) == (B))) || ((DType[this]) == (C)));
call A_A(this, msgsender_MSG, msgvalue_MSG, a_s19);
while (true)
{
havoc msgsender_MSG;
havoc msgvalue_MSG;
havoc choice;
havoc a_s19;
}
}

procedure BoogieEntry_B();
implementation BoogieEntry_B()
{
var this: Ref;
var msgsender_MSG: Ref;
var msgvalue_MSG: int;
var choice: int;
var a_s19: int;
var a_s42: int;
assume (((DType[this]) == (B)) || ((DType[this]) == (C)));
call B_B(this, msgsender_MSG, msgvalue_MSG, a_s42);
while (true)
{
havoc msgsender_MSG;
havoc msgvalue_MSG;
havoc choice;
havoc a_s19;
havoc a_s42;
}
}

procedure BoogieEntry_C();
implementation BoogieEntry_C()
{
var this: Ref;
var msgsender_MSG: Ref;
var msgvalue_MSG: int;
var choice: int;
var a_s19: int;
var a_s42: int;
var a_s65: int;
assume ((DType[this]) == (C));
call C_C(this, msgsender_MSG, msgvalue_MSG, a_s65);
while (true)
{
havoc msgsender_MSG;
havoc msgvalue_MSG;
havoc choice;
havoc a_s19;
havoc a_s42;
havoc a_s65;
}
}

procedure CorralChoice_A(this: Ref);
implementation CorralChoice_A(this: Ref)
{
var msgsender_MSG: Ref;
var msgvalue_MSG: int;
var choice: int;
var a_s19: int;
havoc msgsender_MSG;
havoc msgvalue_MSG;
havoc choice;
havoc a_s19;
}

procedure CorralEntry_A();
implementation CorralEntry_A()
{
var this: Ref;
var msgsender_MSG: Ref;
var msgvalue_MSG: int;
var a_s19: int;
assume ((((DType[this]) == (A)) || ((DType[this]) == (B))) || ((DType[this]) == (C)));
call A_A(this, msgsender_MSG, msgvalue_MSG, a_s19);
while (true)
{
call CorralChoice_A(this);
}
}

procedure CorralChoice_B(this: Ref);
implementation CorralChoice_B(this: Ref)
{
var msgsender_MSG: Ref;
var msgvalue_MSG: int;
var choice: int;
var a_s19: int;
var a_s42: int;
havoc msgsender_MSG;
havoc msgvalue_MSG;
havoc choice;
havoc a_s19;
havoc a_s42;
}

procedure CorralEntry_B();
implementation CorralEntry_B()
{
var this: Ref;
var msgsender_MSG: Ref;
var msgvalue_MSG: int;
var a_s42: int;
assume (((DType[this]) == (B)) || ((DType[this]) == (C)));
call B_B(this, msgsender_MSG, msgvalue_MSG, a_s42);
while (true)
{
call CorralChoice_B(this);
}
}

procedure CorralChoice_C(this: Ref);
implementation CorralChoice_C(this: Ref)
{
var msgsender_MSG: Ref;
var msgvalue_MSG: int;
var choice: int;
var a_s19: int;
var a_s42: int;
var a_s65: int;
havoc msgsender_MSG;
havoc msgvalue_MSG;
havoc choice;
havoc a_s19;
havoc a_s42;
havoc a_s65;
}

procedure CorralEntry_C();
implementation CorralEntry_C()
{
var this: Ref;
var msgsender_MSG: Ref;
var msgvalue_MSG: int;
var a_s65: int;
assume ((DType[this]) == (C));
call C_C(this, msgsender_MSG, msgvalue_MSG, a_s65);
while (true)
{
call CorralChoice_C(this);
}
}


