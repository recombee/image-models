import sys
import json
import numpy as np
from glob import glob
from keras.preprocessing import image
from keras.applications.vgg16 import VGG16
from os.path import basename, isfile, splitext
from keras.applications.vgg16 import preprocess_input

#Allows cross-process parallelization
def ParallelProcess(inputFiles, dstPath, dstFileNameFunc, actionFunc, dumpFunc = None):
  if dumpFunc is None:
    dumpFunc = np.save;

  for inFile in inputFiles:
    f = basename(inFile)
    dst = dstPath + dstFileNameFunc(f);

    if isfile(dst):
      print("Skipping " + f)
      continue

    with open(dst, 'w'):
      pass

    print(f)
    res = actionFunc(inFile);
    if res is None:
      print("..None");
      os.remove(dst);
    else:
      dumpFunc(dst, res);

def DumpJSON(file, data):
  with open(file, 'w') as outfile:
    json.dump(data, outfile)

baseModel = VGG16(weights='imagenet', include_top=False)
def ProcessImage(imageFile):
  #Load image file
  imagePixels = image.load_img(imageFile, target_size=(224, 224))

  #Conver to array, for NN input
  x = image.img_to_array(imagePixels)
  imageArray = np.expand_dims(x, axis=0)

  #Send to NN (consider batching for marginal perf boost)
  features = baseModel.predict(imageArray)

  I = -1
  res = []

  #Features are quite sparse (lot of zeros)
  #=> Extract sparse vectors to save on memory
  for x in features.reshape((25088)):
    I = I + 1
    if x == 0:
      continue

    res.append({"I": I, "V": float(x)});

  return res

args = sys.argv
SRC_PATH = args[1] + "\\"
DST_PATH = args[2] + "\\"

print("Processing images")
print(SRC_PATH + " -> " + DST_PATH)

images = glob(SRC_PATH + "*")
ParallelProcess(images, DST_PATH, lambda x: x.split('.')[0] + ".json", ProcessImage, DumpJSON)



