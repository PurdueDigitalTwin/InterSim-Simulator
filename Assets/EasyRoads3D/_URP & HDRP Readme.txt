
Unity URP and HDRP projects require shaders built for that specific render pipeline. When the shader does not match the render pipeline the material will render in pink.

The EasyRoads3D Demo package includes separate material packages for URP and HDRP projects. These packages are located in:

/Assets/EasyRoads3D/SRP Support packages/ 

When the demo package is imported, scripts run that will automatically import the required material package.

Should you still see pink materials, for example when the main EasyRoads3D Pro package was imported first, then the specific URP or HDRP package can be imported manually. This is quickly done by selecting the package from inside the Unity editor in the project window at the path also mentioned above:/Assets/EasyRoads3D/SRP Support packages/ 

Important:

1. URP and HDRP support was added in v3.2. All road network related materials in the v3.2+ Demo Scene will be converted. The road network related materials in the v3.1 demo scene use older shaders and will still appear in pink. In the case you want to use these older assets, one of the new v3.2 shaders can be assigned to these materials.

2. The URP and HDRP packages include materials that have custom EasyRoads3D shaders assigned. Materials that use Unity built-in shaders can be upgraded the standard way, with the Unity Render Pipeline Converter: https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@12.0/manual/features/rp-converter.html

  
Please contact us should you still have material issues after following the above steps so we can help resolving this.

Forum: http://forum.unity3d.com/threads/easyroads3d-v3-the-upcoming-new-road-system.229327/
Website: http://www.easyroads3d.com
Support: info@easyroads3d.com