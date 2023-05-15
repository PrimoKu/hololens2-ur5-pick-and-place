using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;

public class Rotation {

    public double x {get; set;}
    public double y {get; set;}
    public double z {get; set;}
    public double w {get; set;}

    public Rotation() {
        this.x = 0.0;
        this.y = 0.0;
        this.z = 0.0;
        this.w = 0.0;
    }

    public Rotation(double x, double y, double z, double w)
    {
        this.x = Math.Round(x, 3);
        this.y = Math.Round(y, 3);
        this.z = Math.Round(z, 3);
        this.w = Math.Round(w, 3);
    }

}