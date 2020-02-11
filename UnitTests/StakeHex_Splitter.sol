pragma solidity >=0.4.24 <0.6.0;

contract HEX {
    function xfLobbyEnter(address referrerAddr)
    external
    payable;

    function xfLobbyExit(uint256 enterDay, uint256 count)
    external;

    function xfLobbyPendingDays(address memberAddr)
    external
    view
    returns (uint256[2] memory words);

    function balanceOf (address account)
    external
    view
    returns (uint256);

    function transfer (address recipient, uint256 amount)
    external
    returns (bool);

    function currentDay ()
    external
    view
    returns (uint256);
}

contract StakeHexReferralSplitter {

    event DistributedShares(
        uint40 timestamp,
        address indexed memberAddress,
        uint256 amount
    );

    HEX internal hx = HEX(0x2b591e99afE9f32eAA6214f7B7629768c40Eeb39);

    address payable internal PHIL = address(0xE9DED01D21C6DDbec4D56c1822dE41E511EdfF34); // 12.5
    address payable internal KEVIN = address(0x5a952b3501c827Ef96412C5CA61418DF93d955C4); // 15
    address payable internal NODE_NONCE = address(0x072297fC12fca02f184e6bD7B99697b7490e5aBf); // 5
    address payable internal MICHAEL = address(0xe551072153c02fa33d4903CAb0435Fb86F1a80cb); // 15
    address payable internal PAUL = address(0x7f4F3E2c70D4FEE9cf9798F3d57629B5a1B5AF46); // 40
    address payable internal KYLE = address(0xD30BC4859A79852157211E6db19dE159673a67E2); // 12.5

    function distribute () public
    {

        assert(hx == HEX(0x2b591e99afE9f32eAA6214f7B7629768c40Eeb39));


        uint256 balance = hx.balanceOf(address(this));
        uint256 fivePercent;
        uint256 previousPercent;
        uint256 prevBalance;

        if(balance > 99){
            previousPercent = fivePercent;
            fivePercent = balance / 20;
            assert(fivePercent > previousPercent);

            hx.transfer(NODE_NONCE, fivePercent); // 5%
            hx.transfer(MICHAEL, 3*fivePercent); // 15%
            hx.transfer(KEVIN, 3*fivePercent); // 15%
            hx.transfer(PAUL, 8*fivePercent); // 40%

            prevBalance = balance;
            assert(prevBalance == balance);

            balance = balance - (15 * fivePercent); // 100% - 15*5% = 25%
            hx.transfer(PHIL, balance / 2); // floor(12.5%)
            hx.transfer(KYLE, balance - (balance / 2)); // ceil(12.5%)
        }else {
            assert(balance < 99);
        }
    }
}