

uniform sampler2D _Gradient4D;
uniform sampler2D _PermTable1D;
uniform sampler2D _PermTable2D;
uniform float _Frequency, _Lacunarity, _Gain;

float4 fade(float4 t)
{
	return t * t * t * (t * (t * 6 - 15) + 10); // new curve
	//return t * t * (3 - 2 * t); // old curve
}

float perm(float x)
{
	return tex2Dlod(_PermTable1D, float4(x,0,0,0)).a;
}

float4 perm2d(float2 uv)
{
	return tex2Dlod(_PermTable2D, float4(uv,0,0));
}

float grad(float x, float4 p)
{
	float4 g = tex2Dlod(_Gradient4D, float4(x,0,0,0)) * 2.0 - 1.0;
	return dot(g, p);
}

float gradperm(float x, float4 p)
{
	float4 g = tex2Dlod(_Gradient4D, float4(x,0,0,0)) * 2.0 - 1.0;
	return dot(g, p);
}
			
float inoise(float4 p)
{
	float4 P = fmod(floor(p), 256.0);	// FIND UNIT HYPERCUBE THAT CONTAINS POINT
  	p -= floor(p);                      // FIND RELATIVE X,Y,Z OF POINT IN CUBE.
	float4 f = fade(p);                 // COMPUTE FADE CURVES FOR EACH OF X,Y,Z, W
	P = P / 256.0;
	const float one = 1.0 / 256.0;
	
    // HASH COORDINATES OF THE 16 CORNERS OF THE HYPERCUBE
    
    float4 AA = perm2d(P.xy) + P.z;
    
    float AAA = perm(AA.x)+P.w, AAB = perm(AA.x+one)+P.w;
    float ABA = perm(AA.y)+P.w, ABB = perm(AA.y+one)+P.w;
    float BAA = perm(AA.z)+P.w, BAB = perm(AA.z+one)+P.w;
    float BBA = perm(AA.w)+P.w, BBB = perm(AA.w+one)+P.w;
  	
    return lerp(
  				lerp( lerp( lerp( gradperm(AAA, p ),  
                                  gradperm(BAA, p + float4(-1, 0, 0, 0) ), f.x),
                            lerp( gradperm(ABA, p + float4(0, -1, 0, 0) ),
                                  gradperm(BBA, p + float4(-1, -1, 0, 0) ), f.x), f.y),
                                  
                      lerp( lerp( gradperm(AAB, p + float4(0, 0, -1, 0) ),
                                  gradperm(BAB, p + float4(-1, 0, -1, 0) ), f.x),
                            lerp( gradperm(ABB, p + float4(0, -1, -1, 0) ),
                                  gradperm(BBB, p + float4(-1, -1, -1, 0) ), f.x), f.y), f.z),
                            
  				 lerp( lerp( lerp( gradperm(AAA+one, p + float4(0, 0, 0, -1)),
                                   gradperm(BAA+one, p + float4(-1, 0, 0, -1) ), f.x),
                             lerp( gradperm(ABA+one, p + float4(0, -1, 0, -1) ),
                                   gradperm(BBA+one, p + float4(-1, -1, 0, -1) ), f.x), f.y),
                                   
                       lerp( lerp( gradperm(AAB+one, p + float4(0, 0, -1, -1) ),
                                   gradperm(BAB+one, p + float4(-1, 0, -1, -1) ), f.x),
                             lerp( gradperm(ABB+one, p + float4(0, -1, -1, -1) ),
                                   gradperm(BBB+one, p + float4(-1, -1, -1, -1) ), f.x), f.y), f.z), f.w);
}

// fractal sum, range -1.0 - 1.0
float fBm(float4 p, int octaves)
{
	float freq = _Frequency, amp = 0.5;
	float sum = 0;	
	for(int i = 0; i < octaves; i++) 
	{
		sum += inoise(p * freq) * amp;
		freq *= _Lacunarity;
		amp *= _Gain;
	}
	return sum;
}

// fractal abs sum, range 0.0 - 1.0
float turbulence(float4 p, int octaves)
{
	float sum = 0;
	float freq = _Frequency, amp = 1.0;
	for(int i = 0; i < octaves; i++) 
	{
		sum += abs(inoise(p*freq))*amp;
		freq *= _Lacunarity;
		amp *= _Gain;
	}
	return sum;
}

// Ridged multifractal, range 0.0 - 1.0
// See "Texturing & Modeling, A Procedural Approach", Chapter 12
float ridge(float h, float _offset)
{
    h = abs(h);
    h = _offset - h;
    h = h * h;
    return h;
}

float ridgedmf(float4 p, int octaves, float _offset)
{
	float sum = 0;
	float freq = _Frequency, amp = 0.5;
	float prev = 1.0;
	for(int i = 0; i < octaves; i++) 
	{
		float n = ridge(inoise(p*freq), _offset);
		sum += n*amp*prev;
		prev = n;
		freq *= _Lacunarity;
		amp *= _Gain;
	}
	return sum;
}
			

