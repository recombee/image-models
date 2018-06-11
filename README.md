Image-Knn recommender
===========
Image-based recommendation model developed within Bachelor's thesis of Martin Pavlicek in RecombeeLab at FIT CTU in Prague.

Project structure
==================
```
ReadMe.txt ............................................................ Brief overview of archive and program manual
|
|-- src................................................................................................ Source codes
|
|-- Bins .......................................................................... Runable version of program files
| |
| |-- 1) ExtractVectors.cmd .............................................. Extracts feature vectors from item images
| |
| |-- 2) ComputeImageKnn.pdf....................................................... Computes KNN neighbours for item
| |
| |-- 3) ShowSimilar.pdf................ Starts interactive dialog presenting similar items based in precomputed KNN
| |
| |-- 4) DownloadInteractions.pdf.......................................... Downloads interaction data from database
| |
| |-- 5) RecommendForUser.pdf .......................................... Starts interactive recommendation for users
| |
| |-- DeepRecommender.exe .................................................... Binary version of reccomender program
| |
| |-- VGG16_FeatureExtractor.py .................................... Python script for item image feature extraction
```

Requirements
===============
.NET Framework 4.6.1 or newer (Windows) OR .NET Core 2.0 or newer (Linux) <br>
Keras neural network framework python package <br>
Any Keras python backend (Tensorflow preferred) <br>
For GPU accelerated computing CUDA 8 or newer is required and TensorflowGPU python package <br>

How to use
==============
Batch files are provided to assist you in bootstrap process. <br>
For help run "DeepRemonneder.exe help". <br>
To show info about specific command run "DeepRecommender.exe [command] help". <br>

Recommendation process is divided into 3 steps:
1) Extraction of feature vectors from images of recommended items
2) Computation of NN neighbours for each recommended item
3) Recommendation for specific user based on Image-KNN algorithm

For quick start follow steps below:
1) Edit provided batch files accoding to your configuration (directories, paths, DB connection string, ...)
2) Run batch files one by one in numbered order
3) For advanced usage run "DeepRecommender.exe [command] (args)"

<b>For more info about this recommendation model see:</b><br>
Pavlíček, Martin. 2018. Doporučovací modely založené na obrázcích. České vysoké učení technické v Praze, Fakulta informačních
technologií