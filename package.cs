package Server_SelectiveSwimming
{
	function createMission ()
	{
		Parent::createMission ();
		SelectiveSwimming_init ();
	}

	function GameConnection::createPlayer ( %client, %spawnPoint )
	{
		Parent::createPlayer (%client, %spawnPoint);

		%player = %client.player;

		if ( isObject (%player) )
		{
			%player.canAttachSwimZone = true;
			SelectiveSwimmingSO.createSwimZone (%player);
		}
	}

	function Armor::onAdd ( %this, %obj )
	{
		Parent::onAdd (%this, %obj);

		%client = %obj.client;

		if ( isObject (%client) )
		{
			SelectiveSwimmingSO.createSwimZone (%obj);
		}
	}

	function Armor::onRemove ( %this, %obj )
	{
		Parent::onRemove (%this, %obj);

		%swimZone = %obj.selSwimZone;

		if ( isObject (%swimZone) )
		{
			SelectiveSwimmingSO.deleteSwimZone (%swimZone);
		}
	}

	function Armor::onNewDataBlock ( %this, %obj )
	{
		Parent::onNewDataBlock (%this, %obj);

		%swimZone = %obj.selSwimZone;

		if ( isObject (%swimZone) )
		{
			SelectiveSwimmingSO.updateSwimZoneScale (%swimZone);
		}
	}

	function SceneObject::setScale ( %this, %scale )
	{
		Parent::setScale (%this, %scale);

		%swimZone = %this.selSwimZone;

		if ( isObject (%swimZone) )
		{
			SelectiveSwimmingSO.updateSwimZoneScale (%swimZone);
		}
	}
};
activatePackage (Server_SelectiveSwimming);
