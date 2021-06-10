//* Handlers for different object callbacks *//

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

// Main package
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

	function SceneObject::setScale ( %this, %scale )
	{
		Parent::setScale (%this, %scale);
		SelectiveSwimmingSO.onObjectBoundsChange (%this);
	}
};
activatePackage (Server_SelectiveSwimming);

// Package for hooking into onAdd, onRemove, and onNewDataBlock callbacks.
//
// We have to activate this package later or else the isFunction() checks performed in
// SelectiveSwimming_init() won't work.
package Server_SelectiveSwimming__callbacks
{
	//* Player and AIPlayer callbacks *//

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

	//* Item callbacks *//

	function ItemData::onAdd ( %this, %obj )
	{
		Parent::onAdd (%this, %obj);
		SelectiveSwimmingSO.onObjectAdd (%obj);
	}

	function ItemData::onRemove ( %this, %obj )
	{
		Parent::onRemove (%this, %obj);
		SelectiveSwimmingSO.onObjectRemove (%obj);
	}

	function ItemData::onNewDataBlock ( %this, %obj )
	{
		Parent::onNewDataBlock (%this, %obj);
		SelectiveSwimmingSO.onObjectBoundsChange (%obj);
	}

	//* Vehicle callbacks *//

	// We have to do each individual subclass because the base VehicleData class doesn't call any
	// callbacks (:

	function WheeledVehicleData::onAdd ( %this, %obj )
	{
		Parent::onAdd (%this, %obj);
		SelectiveSwimmingSO.onObjectAdd (%obj);
	}

	function WheeledVehicleData::onRemove ( %this, %obj )
	{
		Parent::onRemove (%this, %obj);
		SelectiveSwimmingSO.onObjectRemove (%obj);
	}

	function WheeledVehicleData::onNewDataBlock ( %this, %obj )
	{
		Parent::onNewDataBlock (%this, %obj);
		SelectiveSwimmingSO.onObjectBoundsChange (%obj);
	}

	function FlyingVehicleData::onAdd ( %this, %obj )
	{
		Parent::onAdd (%this, %obj);
		SelectiveSwimmingSO.onObjectAdd (%obj);
	}

	function FlyingVehicleData::onRemove ( %this, %obj )
	{
		Parent::onRemove (%this, %obj);
		SelectiveSwimmingSO.onObjectRemove (%obj);
	}

	function FlyingVehicleData::onNewDataBlock ( %this, %obj )
	{
		Parent::onNewDataBlock (%this, %obj);
		SelectiveSwimmingSO.onObjectBoundsChange (%obj);
	}

	// I was going to add HoverVehicle callbacks, but the class appears to have been removed...
};
