using NUnit.Framework.Internal;
using System.Threading.Tasks;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    NtfRemoveGem ntfRemoveGem;
    NetworkBuffer buffer;
    NtfRemoveGem test;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ntfRemoveGem = new NtfRemoveGem();
        ntfRemoveGem.newScore = 999;
        ntfRemoveGem.removePlayer = 188;
        ntfRemoveGem.toRemove.Add(0);
        ntfRemoveGem.toRemove.Add(1);
        ntfRemoveGem.toRemove.Add(2);
        ntfRemoveGem.toRemove.Add(3);

        test = new NtfRemoveGem();

        buffer = new NetworkBuffer(1024 * 64, false);

    }

    private void Update() {
        PacketSerializer.serializePacket(ntfRemoveGem, buffer);

        NtfRemoveGem res = (NtfRemoveGem)PacketSerializer.deserializePacket(buffer);

        Debug.Log("!");
    }
    async public void AsyncTest() {
        await Task.Delay(1000);
        Debug.Log("ahahah");
        await Task.Delay(1000);
        Debug.Log("ahahah2");
    }
}
