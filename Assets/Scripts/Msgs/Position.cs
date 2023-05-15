using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;

public class Position {

    public double x {get; set;}
    public double y {get; set;}
    public double z {get; set;}

    public Position() {
        this.x = 0.0;
        this.y = 0.0;
        this.z = 0.0;
    }

    public Position(double x, double y, double z) {
        this.x = Math.Round(x, 3);
        this.y = Math.Round(y, 3);
        this.z = Math.Round(z, 3);
    }
}