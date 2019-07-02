﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ScapeNetLib;
using Lidgren.Network;

[RequireComponent(typeof(ScapeNet_Identifier))]
public class ScapeNet_Server : MonoBehaviour
{

    public Networker_Server_Unity serverNetworker;
    private ScapeNet_Identifier identifier;

    void Awake(){
        DontDestroyOnLoad(this);

        serverNetworker = new Networker_Server_Unity();
        identifier = GetComponent<ScapeNet_Identifier>();

        serverNetworker.Setup("Forts", 7777);
        serverNetworker.HostServer(100,10, "secret");    
    }
   
    // Update is called once per frame
    void Update()
    {
        serverNetworker.Update();
    }

     public void SendPacketToAll<T>(T packet) where T : Packet<T>{
        serverNetworker.SendPacketToAll(packet, 999);
    }

    public void IssueSpawnCommand(string obj_name, Vector3 position){

        InstantiationPacket packet = new InstantiationPacket("D_Instantiate");
        packet.obj_name = obj_name;
        packet.item_net_id = serverNetworker.GetNextItemID();
        packet.x = position.x;
        packet.y = position.y;
        packet.z = position.z;

        serverNetworker.SendPacketToAll(packet, 999);
        serverNetworker.AddRegister(packet, 999);
    }

     public void IssueSpawnCommand(string obj_name, Vector3 position, int playerId){

        InstantiationPacket packet = new InstantiationPacket("D_Instantiate");
        packet.obj_name = obj_name;
        packet.item_net_id = serverNetworker.GetNextItemID();
        packet.x = position.x;
        packet.y = position.y;
        packet.z = position.z;

        serverNetworker.SendPacketToAll(packet, playerId);
        serverNetworker.AddRegister(packet, playerId);
    }

    public void SpawnServerside(string obj_name, Vector3 position){

        int itemId = serverNetworker.GetNextItemID();

        GameObject newObj = null;
        newObj = Instantiate(FindNetObject(obj_name), position, Quaternion.identity);
        newObj.GetComponent<ScapeNet_Network_ID>().players_id = 999;
        newObj.GetComponent<ScapeNet_Network_ID>().object_id = itemId;

        InstantiationPacket packet = new InstantiationPacket("D_Instantiate");
        packet.obj_name = obj_name;
        packet.item_net_id = itemId;
        packet.x = position.x;
        packet.y = position.y;
        packet.z = position.z;

        identifier.currentAliveNetworkedObjects.Add(newObj);
        serverNetworker.SendPacketToAll(packet, 999);
        serverNetworker.AddRegister(packet, 999);
    }


    public GameObject FindNetObject(string object_name){
        foreach(GameObject go in identifier.networkableObjects)
            if(go != null && go.name == object_name)
                return go;

        return null;
    }

     public GameObject FindSpawnedNetObject(int obj_net_id){
          foreach(GameObject go in identifier.currentAliveNetworkedObjects)
            if(go != null && go.GetComponent<ScapeNet_Network_ID>().object_id == obj_net_id)
                return go;

        return null;
    }

    public void OnApplicationQuit()
    {
        serverNetworker.Close();
    }
}
