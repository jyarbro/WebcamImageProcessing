v9 still targets WinUI3 just like v8. The big difference is starting to rework the architecture of the filters to allow for different image source data, i.e. using files vice webcam frames. This requires abstracting out the filtering from the frame capturing.

This middleware approach will ideally enable storage of image data as well, leading to a basis for practicing machine learning concepts.

If you are looking for code to work with a Kinect, look at previous versions (I think v7 and earlier)
