using ThinkFun.Model;

namespace ThinkFun.Server.Sources.ThemeParkWiki;

public class ThemeParkWikiSource
    : IDataSource
{
    static string ExternalIdKey = "tpw";
    HttpClient Client;

    public ThemeParkWikiSource()
    {
        Client = new()
        {
            BaseAddress = new Uri("https://api.themeparks.wiki/v1/")
        };
    }

    ~ThemeParkWikiSource()
    {
        Client.Dispose();
    }

    public async Task Update(DataCollection collection, CancellationToken t)
    {
        // Get the user information.
        var destinations = await Client.GetFromJsonAsync< DestinationList >("destinations", t);
        foreach(var destination in destinations.destinations)
        {
            //for debug, only disney :D
            if (destination.slug != "disneylandparis")
                continue;

            var destination_as_entity = await Client.GetFromJsonAsync<Entity>("entity/"+destination.id, t);

            var destination_model = new Model.Destination
            {
                UniqueIdentifier = destination.slug,
                Name = destination.name,
                ExternalIds = new Dictionary<string, string>
                {
                    {ExternalIdKey, destination_as_entity.id}
                }
            };

            if (destination_as_entity.location != null)
                destination_model.Position = new Model.Position
                {
                    Latitude = destination_as_entity.location.latitude,
                    Longitude = destination_as_entity.location.longitude
                };

            foreach(var park in destination.parks)
            {
                var park_as_entity = await Client.GetFromJsonAsync<Entity>("entity/" + park.id, t);

                var park_as_model = new Model.Park
                {
                    UniqueIdentifier = park.id,
                    Name = park.name,
                    ParentId = destination_model.UniqueIdentifier,
                    ExternalIds = new Dictionary<string, string>
                    {
                        {ExternalIdKey,  park.name},
                    },
                    Position = new Model.Position
                    {
                        Latitude = park_as_entity.location.latitude,
                        Longitude = park_as_entity.location.longitude
                    }
                };
                collection.Set(park_as_model);

                if (destination_as_entity.location == null && park_as_entity.location != null)
                    destination_model.Position = new Model.Position
                    {
                        Latitude = park_as_entity.location.latitude,
                        Longitude = park_as_entity.location.longitude
                    };

                var attractions = await Client.GetFromJsonAsync<ChildenList>("entity/" + park.id + "/children", t);
                foreach (var attraction in attractions.children)
                {
                    var attraction_as_entity = await Client.GetFromJsonAsync<Entity>("entity/" + attraction.id, t);

                    Model.ParkElement attraction_as_model = null;
                    if (attraction.entityType == "ATTRACTION")
                    {
                        attraction_as_model = new Model.Attraction
                        {
                            Name = attraction_as_entity.name,
                            UniqueIdentifier = attraction_as_entity.id,
                            ParentId = park_as_model.UniqueIdentifier,
                            Position = new Model.Position
                            {
                                Latitude = attraction_as_entity.location.latitude,
                                Longitude = attraction_as_entity.location.longitude
                            }
                        };
                    }
                    else if (attraction.entityType == "RESTAURANT")
                    {
                        attraction_as_model = new Model.Restaurant
                        {
                            Name = attraction_as_entity.name,
                            UniqueIdentifier = attraction_as_entity.id,
                            ParentId = park_as_model.UniqueIdentifier,
                            Position = new Model.Position
                            {
                                Latitude = attraction_as_entity.location.latitude,
                                Longitude = attraction_as_entity.location.longitude
                            }
                        };
                    }
                    else if (attraction.entityType == "SHOW")
                    {
                        attraction_as_model = new Model.Show
                        {
                            Name = attraction_as_entity.name,
                            UniqueIdentifier = attraction_as_entity.id,
                            ParentId = park_as_model.UniqueIdentifier
                        };
                    }
                    else
                        continue;

                    collection.Set(attraction_as_model);
                }
            }

            collection.Set(destination_model);
        }

    }

    public async Task UpdateLiveData(DataCollection collection, CancellationToken t)
    {
        foreach(var destination in collection.Destinations)
        {
            if (!destination.ExternalIds.ContainsKey(ExternalIdKey))
                continue;
            string destinationId = destination.ExternalIds[ExternalIdKey];

            var liveDataResponse = await Client.GetFromJsonAsync<LiveDataResponse>("entity/" + destinationId + "/live", t);
            
            foreach(var livedata in liveDataResponse.liveData)
            {
                var livedata_model = new Model.Queue
                {
                    LastUpdate = livedata.lastUpdated,
                    ParkElementId = livedata.id,
                    ParkId = livedata.parkId,
                    DestinationId = destination.UniqueIdentifier
                };

                if (livedata.status == "OPERATING")
                    livedata_model.Status = Status.OPENED;
                else if (livedata.status == "DOWN")
                    livedata_model.Status = Status.DOWN;
                else
                    livedata_model.Status = Status.CLOSED;

                if (livedata.queue != null)
                {
                    if (livedata.queue.STAND_BY != null && livedata.queue.STAND_BY.waitTime != null)
                        livedata_model.ClassicWaitTime = TimeSpan.FromMinutes(livedata.queue.STAND_BY.waitTime.Value);

                    if (livedata.queue.SINGLE_RIDER != null && livedata.queue.SINGLE_RIDER.waitTime != null)
                        livedata_model.SingleRiderWaitTime = TimeSpan.FromMinutes(livedata.queue.SINGLE_RIDER.waitTime.Value);
                }


                collection.Add(livedata_model);
            }
        }
    }
}
