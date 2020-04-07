pragma solidity >=0.4.24 <0.6.0;

contract SimpleToken {
    uint public initialSupply;

    mapping(address=>uint) balances;
    
    constructor(uint _initialSupply) public {
        initialSupply = _initialSupply;
        balances[msg.sender] = _initialSupply;
    }
    
    function sendFunds(address _recipient, uint _amount) public {
        require(balances[msg.sender] >= _amount, "Funds are insufficient for this request");
        require(_recipient != msg.sender, "No need to send tokens to yourself");
        require(balances[_recipient] + _amount > balances[_recipient]); //overflow check
        balances[msg.sender] -= _amount;
        balances[_recipient] += _amount;
    }

    function balanceOf(address _owner) public view returns (uint) {
        return balances[_owner];
    }
}