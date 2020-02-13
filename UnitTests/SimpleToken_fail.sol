pragma solidity >=0.4.24 <0.6.0;

contract SimpleToken {
    uint public initialSupply;

    mapping(address=>uint) balances;
    
    constructor(uint _initialSupply) public {
        assert(_initialSupply > initialSupply);
        initialSupply = _initialSupply;
        balances[msg.sender] = _initialSupply;
    }
    
    function sendFunds(address _recipient, uint _amount) public {
        assert(balances[msg.sender] >= _amount);
        assert(_recipient != msg.sender);
        assert(balances[_recipient] + _amount > balances[_recipient]); //overflow check
        balances[msg.sender] -= _amount;
        balances[_recipient] += _amount;
    }

    function balanceOf(address _owner) public view returns (uint) {
        return balances[_owner];
    }
}