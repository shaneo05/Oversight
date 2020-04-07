/**
 *Submitted for verification at Etherscan.io on 2020-01-15
*/

pragma solidity >=0.4.24 <0.6.0;

library SafeMath {
  function mul(uint256 a, uint256 b) internal pure returns (uint256) {
    if (a == 0) {
      return 0;
    }
    uint256 c = a * b;
    assert(c / a == b);
    return c;
  }

  function div(uint256 a, uint256 b) internal pure returns (uint256) {
    assert(b > 0); // Solidity automatically throws when dividing by 0
    uint256 c = a / b;
    assert(a == b * c + a % b); // There is no case in which this doesn't hold
    return c;
  }

  function sub(uint256 a, uint256 b) internal pure returns (uint256) {
    assert(b <= a);
    return a - b;
  }

  function add(uint256 a, uint256 b) internal pure returns (uint256) {
    uint256 c = a + b;
    assert(c >= a);
    return c;
  }
}

contract KOLToken {
    
    using SafeMath for uint256;
    
    string public name = "Global Short Video &Netcasting KOL Community Alliance";      //  token name
    
    string public symbol = "KOL";           //  token symbol
    
    uint256 public decimals = 18;            //  token digit

    mapping (address => uint256) public balanceOf;
    
    mapping (address => mapping (address => uint256)) public allowance;
 
    
    uint256 public totalSupply = 0;

    uint256 constant valueFounder = 210000000000000000000000000;

    address constant public testAddress = address(0xE0f5206BBD039e7b0592d8918820024e2a7437b9);
    
    address payable private testPayable;

    modifier validAddress {

         testPayable = address(uint160(testAddress));
         assert(address(0x0) != testPayable);
         _;
    }
    
    event Transfer(address indexed _from, address indexed _to, uint256 _value);
    
    event Approval(address indexed _owner, address indexed _spender, uint256 _value);
    event Burn(address indexed _from , uint256 _value);
    
    constructor() public {

        totalSupply = valueFounder;
        balanceOf[testPayable] = valueFounder;
        assert(totalSupply == valueFounder);                            
        emit Transfer(address(0x0), testPayable, valueFounder);
    }
    
    function _transfer(address _from, address _to, uint256 _value) private {
        require(_to != address(0x0));
        require(balanceOf[_from] >= _value);
        balanceOf[_from] = balanceOf[_from].sub(_value);
        balanceOf[_to] = balanceOf[_to].add(_value);
        emit Transfer(_from, _to, _value);
    }
    
    function transfer(address _to, uint256 _value) validAddress public returns (bool success) {
        _transfer(testPayable, _to, _value);
        return true;
    }

    function transferFrom(address _from, address _to, uint256 _value) validAddress public returns (bool success) {
        assert(_from != _to);
        require(_value <= allowance[_from][testPayable]);
        allowance[_from][testPayable] = allowance[_from][testPayable].sub(_value);
        _transfer(_from, _to, _value);
        return true;
    }

    function approve(address _spender, uint256 _value) validAddress public returns (bool success) {
        require(balanceOf[testPayable] >= _value);
        allowance[testPayable][_spender] = _value;
        emit Approval(testPayable, _spender, _value);
        return true;
    }
    function burn(uint256 _value) validAddress public returns (bool success) {
        require (balanceOf[testPayable] >= _value);// Check if the sender has enough
        require (_value > 0);
        balanceOf[testPayable] = balanceOf[testPayable].sub(_value);           // Subtract from the sender
        totalSupply = totalSupply.sub(_value);                       // Updates totalSupply
        emit Burn(testPayable, _value);
        return true;
    }
}