
# Load packages needed for DeployR
require(deployrUtils)
deployrPackage("RevoScaleR")

load("pricingModel.rda")
load("inputExample.rda")

# age, gender and product selected are inputs to the service
inputExample[1,]$age <- as.numeric(age)
inputExample[1,]$gender <- gender
inputExample[1,]$productSelected <- productSelected

prediction <- rxPredict(pricingModel, data = inputExample)
