pragma solidity >=0.4.24<0.6.0;

//import "C:/OversightRepo/Oversight/Project/UnitTests/ValidationSpecs.sol";

//Simple DAO to facilitate reentrancy bug
contract basicDAO {
    mapping (address => uint) public credit;

    constructor() public {
    }
    function donate() payable public {
        credit[msg.sender] += msg.value;
    }
    function queryCredit(address to) public view returns (uint) {
        return credit[to];
    }
    function withdraw() public {
        //Checks to be converted into Boogie, condition checks whether the current balance is always greater or equal to the previous balance - credit value. 
        //OverSight.postCondition_Check(address(this).balance >= OverSight.Old(address(this).balance - credit[msg.sender])); 

        assert(address(this).balance >= address(this).balance - credit[msg.sender]);

        //OverSight.Ensures(address(this).balance >= OverSight.Old(address(this).balance - credit[msg.sender]));          

        uint oldBal = address(this).balance; 
        uint amount = credit[msg.sender];
        if (amount > 0) {
            bool success;
            bytes memory status;
            (success, status) = msg.sender.call.value(amount)(""); 
            require(success);
            credit[msg.sender] = 0;  //cause of reentrancy bug,
            //To fix this exploit, place the assignment at beginnning of conditional.
        }
       uint bal = address(this).balance;
    }
}
