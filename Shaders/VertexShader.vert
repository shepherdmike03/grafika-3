#version 330 core
layout (location = 0) in vec3 vPos;
layout (location = 1) in vec4 vCol;
layout (location = 2) in vec3 vNormal;

uniform mat4 uModel;
uniform mat3 uNormal;
uniform mat4 uView;
uniform mat4 uProjection;
uniform bool uUsePerpendicularNormals;

out vec4 outCol;
out vec3 outNormal;
out vec3 outWorldPosition;
        
void main()
{
	outCol = vCol;
    gl_Position = uProjection*uView*uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0);
    if(uUsePerpendicularNormals)
    {
        outNormal = uNormal*vNormal;
    }
    else
    {
        float angle = radians(10.0); // Convert 10 degrees to radians
        mat3 rotationMatrix = mat3(cos(angle), 0, sin(angle), 0, 1, 0, -sin(angle), 0, cos(angle));
        outNormal = rotationMatrix * vNormal;
    }

    outWorldPosition = vec3(uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0));
}