mergeInto(LibraryManager.library, {

    // Store connected address
    _connectedAddress: null,
    _web3Provider: null,

    JS_ConnectWallet: function(gameObjectNamePtr, callbackMethodPtr) {
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var callbackMethod = UTF8ToString(callbackMethodPtr);

        // Check if Web3 is available
        if (typeof window.ethereum !== 'undefined') {
            // MetaMask or similar
            window.ethereum.request({ method: 'eth_requestAccounts' })
                .then(function(accounts) {
                    if (accounts.length > 0) {
                        _connectedAddress = accounts[0];
                        SendMessage(gameObjectName, callbackMethod, accounts[0]);
                    } else {
                        SendMessage(gameObjectName, callbackMethod, 'error:No accounts found');
                    }
                })
                .catch(function(error) {
                    SendMessage(gameObjectName, callbackMethod, 'error:' + error.message);
                });
        } else if (typeof window.WalletConnectProvider !== 'undefined') {
            // WalletConnect
            var provider = new WalletConnectProvider.default({
                rpc: {
                    1: "https://mainnet.infura.io/v3/YOUR_INFURA_KEY"
                }
            });

            provider.enable()
                .then(function(accounts) {
                    _web3Provider = provider;
                    _connectedAddress = accounts[0];
                    SendMessage(gameObjectName, callbackMethod, accounts[0]);
                })
                .catch(function(error) {
                    SendMessage(gameObjectName, callbackMethod, 'error:' + error.message);
                });
        } else {
            SendMessage(gameObjectName, callbackMethod, 'error:No Web3 provider found');
        }
    },

    JS_DisconnectWallet: function() {
        _connectedAddress = null;
        if (_web3Provider && _web3Provider.disconnect) {
            _web3Provider.disconnect();
        }
        _web3Provider = null;
    },

    JS_GetConnectedAddress: function() {
        if (_connectedAddress) {
            var bufferSize = lengthBytesUTF8(_connectedAddress) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(_connectedAddress, buffer, bufferSize);
            return buffer;
        }
        return null;
    },

    JS_CheckNFTOwnership: function(contractAddressPtr, tokenIdPtr, gameObjectNamePtr, callbackMethodPtr) {
        var contractAddress = UTF8ToString(contractAddressPtr);
        var tokenId = UTF8ToString(tokenIdPtr);
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var callbackMethod = UTF8ToString(callbackMethodPtr);

        if (!_connectedAddress) {
            SendMessage(gameObjectName, callbackMethod, contractAddress + '_' + tokenId + ':false');
            return;
        }

        // This is a simplified example - in production you'd use ethers.js or web3.js
        // to actually query the blockchain
        var key = contractAddress + '_' + tokenId;

        // Mock implementation - replace with actual contract call
        // For ERC-1155: balanceOf(address, tokenId) > 0
        // For ERC-721: ownerOf(tokenId) == address
        SendMessage(gameObjectName, callbackMethod, key + ':true');
    },

    JS_GetOwnedNFTs: function(contractAddressPtr, gameObjectNamePtr, callbackMethodPtr) {
        var contractAddress = UTF8ToString(contractAddressPtr);
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var callbackMethod = UTF8ToString(callbackMethodPtr);

        if (!_connectedAddress) {
            SendMessage(gameObjectName, callbackMethod, '{"nfts":[]}');
            return;
        }

        // Mock implementation - replace with actual indexer/API call
        // You would typically use an API like Alchemy, Moralis, or OpenSea
        var mockResult = {
            nfts: [
                {
                    tokenId: "1",
                    contractAddress: contractAddress,
                    amount: 1,
                    nftType: "ERC1155",
                    metadata: {
                        name: "Test NFT",
                        description: "A test NFT"
                    }
                }
            ]
        };

        SendMessage(gameObjectName, callbackMethod, JSON.stringify(mockResult));
    }
});
