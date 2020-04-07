type Ref;
type ContractName;
const unique null: Ref;
const unique SimpleToken: ContractName;
function ConstantToRef(x: int) returns (ret: Ref);
function {:bvbuiltin "mod"} modBpl(x: int, y: int) returns (ret: int);
function _SumMapping_OverSight(x: [Ref]int) returns (ret: int);
var Balance: [Ref]int;
var DType: [Ref]ContractName;
var Alloc: [Ref]bool;
var balance_ADDR: [Ref]int;
var M_Ref_int: [Ref][Ref]int;
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
procedure {:inline 1} SimpleToken_SimpleToken_NoBaseCtor(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int);
implementation SimpleToken_SimpleToken_NoBaseCtor(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int)
{
var __var_1: Ref;
// start of initialization
assume ((msgsender_MSG) != (null));
initialSupply_SimpleToken[this] := 0;
// Make array/mapping vars distinct for balances
call __var_1 := FreshRefGenerator();
balances_SimpleToken[this] := __var_1;
// Initialize Integer mapping balances
assume (forall  __i__0_0:Ref ::  ((M_Ref_int[balances_SimpleToken[this]][__i__0_0]) == (0)));
// end of initialization
}

procedure {:inline 1} SimpleToken_SimpleToken(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int);
implementation SimpleToken_SimpleToken(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int)
{
call  {:cexpr "_OverSightFirstArg"} boogie_si_record_sol2Bpl_bool(false);
call  {:cexpr "this"} boogie_si_record_sol2Bpl_ref(this);
call  {:cexpr "msg.sender"} boogie_si_record_sol2Bpl_ref(msgsender_MSG);
call  {:cexpr "msg.value"} boogie_si_record_sol2Bpl_int(msgvalue_MSG);
call  {:cexpr "_OverSightLastArg"} boogie_si_record_sol2Bpl_bool(true);
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\SimpleToken.sol"} {:sourceLine 3} (true);
call SimpleToken_SimpleToken_NoBaseCtor(this, msgsender_MSG, msgvalue_MSG);
}

var initialSupply_SimpleToken: [Ref]int;
var balances_SimpleToken: [Ref]Ref;
procedure {:public} {:inline 1} _SimpleToken(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, _initialSupply_s24: int);
implementation _SimpleToken(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, _initialSupply_s24: int)
{
call  {:cexpr "_OverSightFirstArg"} boogie_si_record_sol2Bpl_bool(false);
call  {:cexpr "this"} boogie_si_record_sol2Bpl_ref(this);
call  {:cexpr "msg.sender"} boogie_si_record_sol2Bpl_ref(msgsender_MSG);
call  {:cexpr "msg.value"} boogie_si_record_sol2Bpl_int(msgvalue_MSG);
call  {:cexpr "_initialSupply"} boogie_si_record_sol2Bpl_int(_initialSupply_s24);
call  {:cexpr "_OverSightLastArg"} boogie_si_record_sol2Bpl_bool(true);
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\SimpleToken.sol"} {:sourceLine 8} (true);
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\SimpleToken.sol"} {:sourceLine 9} (true);
assume ((initialSupply_SimpleToken[this]) >= (0));
assume ((_initialSupply_s24) >= (0));
initialSupply_SimpleToken[this] := _initialSupply_s24;
call  {:cexpr "initialSupply"} boogie_si_record_sol2Bpl_int(initialSupply_SimpleToken[this]);
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\SimpleToken.sol"} {:sourceLine 10} (true);
assume ((M_Ref_int[balances_SimpleToken[this]][msgsender_MSG]) >= (0));
assume ((_initialSupply_s24) >= (0));
M_Ref_int[balances_SimpleToken[this]][msgsender_MSG] := _initialSupply_s24;
call  {:cexpr "balances[msg.sender]"} boogie_si_record_sol2Bpl_int(M_Ref_int[balances_SimpleToken[this]][msgsender_MSG]);
}

procedure {:public} {:inline 1} sendFunds_SimpleToken(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, _recipient_s75: Ref, _amount_s75: int);
implementation sendFunds_SimpleToken(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, _recipient_s75: Ref, _amount_s75: int)
{
call  {:cexpr "_OverSightFirstArg"} boogie_si_record_sol2Bpl_bool(false);
call  {:cexpr "this"} boogie_si_record_sol2Bpl_ref(this);
call  {:cexpr "msg.sender"} boogie_si_record_sol2Bpl_ref(msgsender_MSG);
call  {:cexpr "msg.value"} boogie_si_record_sol2Bpl_int(msgvalue_MSG);
call  {:cexpr "_recipient"} boogie_si_record_sol2Bpl_ref(_recipient_s75);
call  {:cexpr "_amount"} boogie_si_record_sol2Bpl_int(_amount_s75);
call  {:cexpr "_OverSightLastArg"} boogie_si_record_sol2Bpl_bool(true);
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\SimpleToken.sol"} {:sourceLine 13} (true);
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\SimpleToken.sol"} {:sourceLine 14} (true);
assume ((M_Ref_int[balances_SimpleToken[this]][msgsender_MSG]) >= (0));
assume ((_amount_s75) >= (0));
assume ((M_Ref_int[balances_SimpleToken[this]][msgsender_MSG]) >= (_amount_s75));
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\SimpleToken.sol"} {:sourceLine 15} (true);
assume ((_recipient_s75) != (msgsender_MSG));
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\SimpleToken.sol"} {:sourceLine 16} (true);
assume ((M_Ref_int[balances_SimpleToken[this]][_recipient_s75]) >= (0));
assume ((_amount_s75) >= (0));
assume (((M_Ref_int[balances_SimpleToken[this]][_recipient_s75]) + (_amount_s75)) >= (0));
assume ((M_Ref_int[balances_SimpleToken[this]][_recipient_s75]) >= (0));
assume (((M_Ref_int[balances_SimpleToken[this]][_recipient_s75]) + (_amount_s75)) > (M_Ref_int[balances_SimpleToken[this]][_recipient_s75]));
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\SimpleToken.sol"} {:sourceLine 17} (true);
assume ((M_Ref_int[balances_SimpleToken[this]][msgsender_MSG]) >= (0));
assume ((_amount_s75) >= (0));
M_Ref_int[balances_SimpleToken[this]][msgsender_MSG] := (M_Ref_int[balances_SimpleToken[this]][msgsender_MSG]) - (_amount_s75);
call  {:cexpr "balances[msg.sender]"} boogie_si_record_sol2Bpl_int(M_Ref_int[balances_SimpleToken[this]][msgsender_MSG]);
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\SimpleToken.sol"} {:sourceLine 18} (true);
assume ((M_Ref_int[balances_SimpleToken[this]][_recipient_s75]) >= (0));
assume ((_amount_s75) >= (0));
M_Ref_int[balances_SimpleToken[this]][_recipient_s75] := (M_Ref_int[balances_SimpleToken[this]][_recipient_s75]) + (_amount_s75);
call  {:cexpr "balances[_recipient]"} boogie_si_record_sol2Bpl_int(M_Ref_int[balances_SimpleToken[this]][_recipient_s75]);
}

procedure {:public} {:inline 1} balanceOf_SimpleToken(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, _owner_s87: Ref) returns (__ret_0_: int);
implementation balanceOf_SimpleToken(this: Ref, msgsender_MSG: Ref, msgvalue_MSG: int, _owner_s87: Ref) returns (__ret_0_: int)
{
call  {:cexpr "_OverSightFirstArg"} boogie_si_record_sol2Bpl_bool(false);
call  {:cexpr "this"} boogie_si_record_sol2Bpl_ref(this);
call  {:cexpr "msg.sender"} boogie_si_record_sol2Bpl_ref(msgsender_MSG);
call  {:cexpr "msg.value"} boogie_si_record_sol2Bpl_int(msgvalue_MSG);
call  {:cexpr "_owner"} boogie_si_record_sol2Bpl_ref(_owner_s87);
call  {:cexpr "_OverSightLastArg"} boogie_si_record_sol2Bpl_bool(true);
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\SimpleToken.sol"} {:sourceLine 21} (true);
assert {:first} {:sourceFile "C:\OversightRepo\Oversight\Project\UnitTests\SimpleToken.sol"} {:sourceLine 22} (true);
assume ((M_Ref_int[balances_SimpleToken[this]][_owner_s87]) >= (0));
__ret_0_ := M_Ref_int[balances_SimpleToken[this]][_owner_s87];
return;
}

procedure BoogieEntry_SimpleToken();
implementation BoogieEntry_SimpleToken()
{
var this: Ref;
var msgsender_MSG: Ref;
var msgvalue_MSG: int;
var choice: int;
var _initialSupply_s24: int;
var _recipient_s75: Ref;
var _amount_s75: int;
var _owner_s87: Ref;
var __ret_0_balanceOf: int;
assume ((DType[this]) == (SimpleToken));
call SimpleToken_SimpleToken(this, msgsender_MSG, msgvalue_MSG);
}


