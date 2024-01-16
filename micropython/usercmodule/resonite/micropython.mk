USERMODULES_DIR := $(USERMOD_DIR)

# Add all C files to SRC_USERMOD.
SRC_USERMOD += $(USERMODULES_DIR)/mp_resonite.c
SRC_USERMOD += $(USERMODULES_DIR)/mp_resonite_slot.c
SRC_USERMOD += $(USERMODULES_DIR)/mp_resonite_utils.c

CFLAGS_USERMOD += -I$(USERMODULES_DIR)

JSFLAGS += --js-library $(USERMODULES_DIR)/resonite_api.js
JSFLAGS += -s WASM_BIGINT
