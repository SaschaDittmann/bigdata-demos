
# Load the trained model and input example
load("pricingModel.rda")
load("inputExample.rda")

# Prepare the input (age, gender and productSelected) to use for prediction
inputExample[1,]$age <- 30
inputExample[1,]$gender <- "F"
inputExample[1,]$productSelected <- "coconut water"

# Execute the prediction
prediction <- predict(pricingModel, data = inputExample)