using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;

public class PoseMsg {

    public string name {get; set;}
    public Position position {get; set;}
    public Rotation rotation {get; set;}

    public PoseMsg() {
        this.name = "";
        this.position = new Position();
        this.rotation = new Rotation();
    }
    public PoseMsg(string name, Position position, Rotation rotation) {
        this.name = name;
        this.position = position;
        this.rotation = rotation;
    }

    public string ToJson() {
        return $"{{\"topic\":\"{this.name}\", " +
                $"\"{nameof(this.position)}\":" + 
                    $"{{\"{nameof(this.position.x)}\":{this.position.x}," + 
                    $"\"{nameof(this.position.y)}\":{this.position.y}," +
                    $"\"{nameof(this.position.z)}\":{this.position.z}}}," + 
                $"\"{nameof(this.rotation)}\":" + 
                    $"{{\"{nameof(this.rotation.x)}\":{this.rotation.x}," + 
                    $"\"{nameof(this.rotation.y)}\":{this.rotation.y}," + 
                    $"\"{nameof(this.rotation.z)}\":{this.rotation.z}," +
                    $"\"{nameof(this.rotation.w)}\":{this.rotation.w}}}"  + "}\n";
    }

}