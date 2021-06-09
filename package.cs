function SelectiveSwimming::onObjectAdd ( %this, %object )
{
	%this.createSwimZone (%object);
}

function SelectiveSwimming::onObjectRemove ( %this, %object )
{
	%swimZone = %object.selSwimZone;

	if ( isObject (%swimZone) )
	{
		SelectiveSwimmingSO.deleteSwimZone (%swimZone);
	}
}

function SelectiveSwimming::onObjectBoundsChange ( %this, %object )
{
	%swimZone = %object.selSwimZone;

	if ( isObject (%swimZone) )
	{
		SelectiveSwimmingSO.updateSwimZoneScale (%swimZone);
	}
}

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
			SelectiveSwimmingSO.onObjectAdd (%player);
		}
	}

	function Armor::onAdd ( %this, %obj )
	{
		Parent::onAdd (%this, %obj);
		SelectiveSwimmingSO.onObjectAdd (%obj);
	}

	function Armor::onRemove ( %this, %obj )
	{
		Parent::onRemove (%this, %obj);
		SelectiveSwimmingSO.onObjectRemove (%obj);
	}

	function Armor::onNewDataBlock ( %this, %obj )
	{
		Parent::onNewDataBlock (%this, %obj);
		SelectiveSwimmingSO.onObjectBoundsChange (%obj);
	}

	function SceneObject::setScale ( %this, %scale )
	{
		Parent::setScale (%this, %scale);
		SelectiveSwimmingSO.onObjectBoundsChange (%this);
	}
};
activatePackage (Server_SelectiveSwimming);
