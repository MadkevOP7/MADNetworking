# MADNetworking for Unity
Unity networking library built on AWS lambda, Dynamodb and APIGateway websocket

## Dependencies
- .Net 6.x
- Unity

## AWS Backend
[AWS Code](https://github.com/MadkevOP7/MADNetworking/tree/main/MADNetworking.AWS)
Contains functions responsible for communicating with DynamoDB, AWS Lambda and APIGateway as example

## Set up in Unity
- Open up any example scene located inside MADNetworking/Examples
- Put your AWS APIGateway websocket connect url into [Websocket URl] field in NetworkManager
- Select your initialization option (auto start or by calling NetworkManager.Instance.StartManager()
- Play around with example Rock Paper Scissors game or view NetworkTransform example and NetworkBehavior example
- Youtube Game Examle: (https://youtu.be/vnzcJQelot0)

## Basic Documentation
- To invoke a function across all clients, make sure your script inherits from [NetworkBehavior](https://github.com/MadkevOP7/MADNetworking/blob/main/MAD%20Networking/Core/NetworkBehaviour.cs), then call NetworkInvoke("FUNCTION_NAME", object[] parameters, bool includeOwner). If includeOwner = true, function will be invoked again on calling client
- NetworkIdentity.isLocalPlayer returns true for current local client that has authority, useful for player control scripts where only the object with authority should respond
- Attach [NetworkTransform](https://github.com/MadkevOP7/MADNetworking/blob/main/MAD%20Networking/Core/Components/NetworkTransform.cs) to any object you wish to sync transform with. Options to sync position, rotation, scale, and specify a sync interval (lower may be more accurate but cost on network usage)
- To spawn an object across all connected clients, use NetworkSpawn(GameObject object), make sure the prefab is registered to NetworkManager before spawning
- To spawn player, add player prefab to [NetworkManager](https://github.com/MadkevOP7/MADNetworking/blob/main/MAD%20Networking/Core/NetworkManager.cs) [Player Prefab] field

