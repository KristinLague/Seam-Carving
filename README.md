# Seam Carving
Seam Carving is this cool technique of resizing an image without disrupting its content too much.

![https://raw.githubusercontent.com/KristinLague/KristinLague.github.io/main/Images/seamcarve.gif](https://raw.githubusercontent.com/KristinLague/KristinLague.github.io/main/Images/seamcarve.gif)

## How does this work? 

In a nut shell what we have to do to make this work is to detect edges in the picture, which can be indicated by a big change in color within a small range of pixels. To do this we are using a what is called a sobel filter or sobel effect. 

For every pixel of the texture we are doing the following operation: 

We imagine a 3x3 grid around the pixel, this is called a kernel. Then we calculate the weighted average color of each row horizontally while giving the row that has our pixel we are looking at double the weight, since this is our point of interest. We then add these three values together to get the average color in horizontal direction. 

Next we are repeating the same operation vertically. Now these two values get convolved.

Once this is done you need to use values of the sobelized (I am sure that is not a word) image to figure out which path from top to bottom of the texture is the least interesting and can be cut. To do we are assigning a value to each pixel that we call energy. if the color is black, I.e. there is no edge there its most likely not interesting and has low energy. Lastly we need to find the path with the lowest energy and remove all the pixel of said path from the texture.
 
