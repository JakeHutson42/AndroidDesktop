/*eslint-disable block-scoped-var, id-length, no-control-regex, no-magic-numbers, no-prototype-builtins, no-redeclare, no-shadow, no-var, sort-vars*/
import * as $protobuf from "protobufjs/minimal";

const $Reader = $protobuf.Reader, $Writer = $protobuf.Writer, $util = $protobuf.util;

const $root = $protobuf.roots["default"] || ($protobuf.roots["default"] = {});

export const android = $root.android = (() => {

    const android = {};

    android.emulation = (function() {

        const emulation = {};

        emulation.control = (function() {

            const control = {};

            control.VmRunState = (function() {

                function VmRunState(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                VmRunState.prototype.state = 0;

                VmRunState.create = function create(properties) {
                    return new VmRunState(properties);
                };

                VmRunState.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.state != null && Object.hasOwnProperty.call(message, "state"))
                        writer.uint32(8).int32(message.state);
                    return writer;
                };

                VmRunState.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.VmRunState();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.state = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                VmRunState.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.VmRunState";
                };

                VmRunState.RunState = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "UNKNOWN"] = 0;
                    values[valuesById[1] = "RUNNING"] = 1;
                    values[valuesById[2] = "RESTORE_VM"] = 2;
                    values[valuesById[3] = "PAUSED"] = 3;
                    values[valuesById[4] = "SAVE_VM"] = 4;
                    values[valuesById[5] = "SHUTDOWN"] = 5;
                    values[valuesById[7] = "TERMINATE"] = 7;
                    values[valuesById[9] = "RESET"] = 9;
                    values[valuesById[10] = "INTERNAL_ERROR"] = 10;
                    values[valuesById[11] = "RESTART"] = 11;
                    values[valuesById[12] = "START"] = 12;
                    values[valuesById[13] = "STOP"] = 13;
                    return values;
                })();

                return VmRunState;
            })();

            control.ParameterValue = (function() {

                function ParameterValue(properties) {
                    this.data = [];
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                ParameterValue.prototype.data = $util.emptyArray;

                ParameterValue.create = function create(properties) {
                    return new ParameterValue(properties);
                };

                ParameterValue.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.data != null && message.data.length) {
                        writer.uint32(10).fork();
                        for (let i = 0; i < message.data.length; ++i)
                            writer.float(message.data[i]);
                        writer.ldelim();
                    }
                    return writer;
                };

                ParameterValue.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.ParameterValue();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                if (!(message.data && message.data.length))
                                    message.data = [];
                                if ((tag & 7) === 2) {
                                    let end2 = reader.uint32() + reader.pos;
                                    while (reader.pos < end2)
                                        message.data.push(reader.float());
                                } else
                                    message.data.push(reader.float());
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                ParameterValue.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.ParameterValue";
                };

                return ParameterValue;
            })();

            control.PhysicalModelValue = (function() {

                function PhysicalModelValue(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                PhysicalModelValue.prototype.target = 0;
                PhysicalModelValue.prototype.status = 0;
                PhysicalModelValue.prototype.value = null;
                PhysicalModelValue.prototype.interpolation = 0;

                PhysicalModelValue.create = function create(properties) {
                    return new PhysicalModelValue(properties);
                };

                PhysicalModelValue.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.target != null && Object.hasOwnProperty.call(message, "target"))
                        writer.uint32(8).int32(message.target);
                    if (message.status != null && Object.hasOwnProperty.call(message, "status"))
                        writer.uint32(16).int32(message.status);
                    if (message.value != null && Object.hasOwnProperty.call(message, "value"))
                        $root.android.emulation.control.ParameterValue.encode(message.value, writer.uint32(26).fork()).ldelim();
                    if (message.interpolation != null && Object.hasOwnProperty.call(message, "interpolation"))
                        writer.uint32(32).int32(message.interpolation);
                    return writer;
                };

                PhysicalModelValue.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.PhysicalModelValue();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.target = reader.int32();
                                break;
                            }
                        case 2: {
                                message.status = reader.int32();
                                break;
                            }
                        case 3: {
                                message.value = $root.android.emulation.control.ParameterValue.decode(reader, reader.uint32());
                                break;
                            }
                        case 4: {
                                message.interpolation = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                PhysicalModelValue.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.PhysicalModelValue";
                };

                PhysicalModelValue.State = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "OK"] = 0;
                    values[valuesById[-3] = "NO_SERVICE"] = -3;
                    values[valuesById[-2] = "DISABLED"] = -2;
                    values[valuesById[-1] = "UNKNOWN"] = -1;
                    return values;
                })();

                PhysicalModelValue.PhysicalType = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "POSITION"] = 0;
                    values[valuesById[1] = "ROTATION"] = 1;
                    values[valuesById[2] = "MAGNETIC_FIELD"] = 2;
                    values[valuesById[3] = "TEMPERATURE"] = 3;
                    values[valuesById[4] = "PROXIMITY"] = 4;
                    values[valuesById[5] = "LIGHT"] = 5;
                    values[valuesById[6] = "PRESSURE"] = 6;
                    values[valuesById[7] = "HUMIDITY"] = 7;
                    values[valuesById[8] = "VELOCITY"] = 8;
                    values[valuesById[9] = "AMBIENT_MOTION"] = 9;
                    values[valuesById[10] = "HINGE_ANGLE0"] = 10;
                    values[valuesById[11] = "HINGE_ANGLE1"] = 11;
                    values[valuesById[12] = "HINGE_ANGLE2"] = 12;
                    values[valuesById[13] = "ROLLABLE0"] = 13;
                    values[valuesById[14] = "ROLLABLE1"] = 14;
                    values[valuesById[15] = "ROLLABLE2"] = 15;
                    values[valuesById[16] = "POSTURE"] = 16;
                    values[valuesById[17] = "HEART_RATE"] = 17;
                    values[valuesById[18] = "RGBC_LIGHT"] = 18;
                    values[valuesById[19] = "WRIST_TILT"] = 19;
                    return values;
                })();

                PhysicalModelValue.Interpolation = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "SMOOTH"] = 0;
                    values[valuesById[1] = "STEP"] = 1;
                    return values;
                })();

                return PhysicalModelValue;
            })();

            control.SensorValue = (function() {

                function SensorValue(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                SensorValue.prototype.target = 0;
                SensorValue.prototype.status = 0;
                SensorValue.prototype.value = null;

                SensorValue.create = function create(properties) {
                    return new SensorValue(properties);
                };

                SensorValue.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.target != null && Object.hasOwnProperty.call(message, "target"))
                        writer.uint32(8).int32(message.target);
                    if (message.status != null && Object.hasOwnProperty.call(message, "status"))
                        writer.uint32(16).int32(message.status);
                    if (message.value != null && Object.hasOwnProperty.call(message, "value"))
                        $root.android.emulation.control.ParameterValue.encode(message.value, writer.uint32(26).fork()).ldelim();
                    return writer;
                };

                SensorValue.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.SensorValue();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.target = reader.int32();
                                break;
                            }
                        case 2: {
                                message.status = reader.int32();
                                break;
                            }
                        case 3: {
                                message.value = $root.android.emulation.control.ParameterValue.decode(reader, reader.uint32());
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                SensorValue.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.SensorValue";
                };

                SensorValue.State = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "OK"] = 0;
                    values[valuesById[-3] = "NO_SERVICE"] = -3;
                    values[valuesById[-2] = "DISABLED"] = -2;
                    values[valuesById[-1] = "UNKNOWN"] = -1;
                    return values;
                })();

                SensorValue.SensorType = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "ACCELERATION"] = 0;
                    values[valuesById[1] = "GYROSCOPE"] = 1;
                    values[valuesById[2] = "MAGNETIC_FIELD"] = 2;
                    values[valuesById[3] = "ORIENTATION"] = 3;
                    values[valuesById[4] = "TEMPERATURE"] = 4;
                    values[valuesById[5] = "PROXIMITY"] = 5;
                    values[valuesById[6] = "LIGHT"] = 6;
                    values[valuesById[7] = "PRESSURE"] = 7;
                    values[valuesById[8] = "HUMIDITY"] = 8;
                    values[valuesById[9] = "MAGNETIC_FIELD_UNCALIBRATED"] = 9;
                    values[valuesById[10] = "GYROSCOPE_UNCALIBRATED"] = 10;
                    values[valuesById[14] = "HEART_RATE"] = 14;
                    values[valuesById[15] = "RGBC_LIGHT"] = 15;
                    values[valuesById[17] = "ACCELERATION_UNCALIBRATED"] = 17;
                    values[valuesById[18] = "HEADING"] = 18;
                    return values;
                })();

                return SensorValue;
            })();

            control.BrightnessValue = (function() {

                function BrightnessValue(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                BrightnessValue.prototype.target = 0;
                BrightnessValue.prototype.value = 0;

                BrightnessValue.create = function create(properties) {
                    return new BrightnessValue(properties);
                };

                BrightnessValue.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.target != null && Object.hasOwnProperty.call(message, "target"))
                        writer.uint32(8).int32(message.target);
                    if (message.value != null && Object.hasOwnProperty.call(message, "value"))
                        writer.uint32(16).uint32(message.value);
                    return writer;
                };

                BrightnessValue.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.BrightnessValue();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.target = reader.int32();
                                break;
                            }
                        case 2: {
                                message.value = reader.uint32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                BrightnessValue.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.BrightnessValue";
                };

                BrightnessValue.LightType = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "LCD"] = 0;
                    values[valuesById[1] = "KEYBOARD"] = 1;
                    values[valuesById[2] = "BUTTON"] = 2;
                    return values;
                })();

                return BrightnessValue;
            })();

            control.DisplayModeValue = (function() {
                const valuesById = {}, values = Object.create(valuesById);
                values[valuesById[0] = "PHONE"] = 0;
                values[valuesById[1] = "FOLDABLE"] = 1;
                values[valuesById[2] = "TABLET"] = 2;
                values[valuesById[3] = "DESKTOP"] = 3;
                return values;
            })();

            control.DisplayMode = (function() {

                function DisplayMode(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                DisplayMode.prototype.value = 0;

                DisplayMode.create = function create(properties) {
                    return new DisplayMode(properties);
                };

                DisplayMode.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.value != null && Object.hasOwnProperty.call(message, "value"))
                        writer.uint32(8).int32(message.value);
                    return writer;
                };

                DisplayMode.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.DisplayMode();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.value = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                DisplayMode.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.DisplayMode";
                };

                return DisplayMode;
            })();

            control.XrOptions = (function() {

                function XrOptions(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                XrOptions.prototype.environment = 0;
                XrOptions.prototype.passthroughCoefficient = 0;
                XrOptions.prototype.dimmingValue = 0;

                XrOptions.create = function create(properties) {
                    return new XrOptions(properties);
                };

                XrOptions.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.environment != null && Object.hasOwnProperty.call(message, "environment"))
                        writer.uint32(8).int32(message.environment);
                    if (message.passthroughCoefficient != null && Object.hasOwnProperty.call(message, "passthroughCoefficient"))
                        writer.uint32(21).float(message.passthroughCoefficient);
                    if (message.dimmingValue != null && Object.hasOwnProperty.call(message, "dimmingValue"))
                        writer.uint32(37).float(message.dimmingValue);
                    return writer;
                };

                XrOptions.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.XrOptions();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.environment = reader.int32();
                                break;
                            }
                        case 2: {
                                message.passthroughCoefficient = reader.float();
                                break;
                            }
                        case 4: {
                                message.dimmingValue = reader.float();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                XrOptions.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.XrOptions";
                };

                XrOptions.Environment = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "LIVING_ROOM_DAY"] = 0;
                    values[valuesById[1] = "LIVING_ROOM_NIGHT"] = 1;
                    return values;
                })();

                return XrOptions;
            })();

            control.LedIndicator = (function() {

                function LedIndicator(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                LedIndicator.prototype.id = null;
                LedIndicator.prototype.facing = null;
                LedIndicator.prototype.state = null;
                LedIndicator.prototype.color = null;

                let $oneOfFields;

                Object.defineProperty(LedIndicator.prototype, "_id", {
                    get: $util.oneOfGetter($oneOfFields = ["id"]),
                    set: $util.oneOfSetter($oneOfFields)
                });

                Object.defineProperty(LedIndicator.prototype, "_facing", {
                    get: $util.oneOfGetter($oneOfFields = ["facing"]),
                    set: $util.oneOfSetter($oneOfFields)
                });

                Object.defineProperty(LedIndicator.prototype, "_state", {
                    get: $util.oneOfGetter($oneOfFields = ["state"]),
                    set: $util.oneOfSetter($oneOfFields)
                });

                Object.defineProperty(LedIndicator.prototype, "_color", {
                    get: $util.oneOfGetter($oneOfFields = ["color"]),
                    set: $util.oneOfSetter($oneOfFields)
                });

                LedIndicator.create = function create(properties) {
                    return new LedIndicator(properties);
                };

                LedIndicator.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.id != null && Object.hasOwnProperty.call(message, "id"))
                        writer.uint32(8).uint32(message.id);
                    if (message.facing != null && Object.hasOwnProperty.call(message, "facing"))
                        writer.uint32(16).int32(message.facing);
                    if (message.state != null && Object.hasOwnProperty.call(message, "state"))
                        writer.uint32(24).int32(message.state);
                    if (message.color != null && Object.hasOwnProperty.call(message, "color"))
                        writer.uint32(32).uint32(message.color);
                    return writer;
                };

                LedIndicator.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.LedIndicator();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.id = reader.uint32();
                                break;
                            }
                        case 2: {
                                message.facing = reader.int32();
                                break;
                            }
                        case 3: {
                                message.state = reader.int32();
                                break;
                            }
                        case 4: {
                                message.color = reader.uint32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                LedIndicator.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.LedIndicator";
                };

                LedIndicator.Facing = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "INSIDE"] = 0;
                    values[valuesById[1] = "OUTSIDE"] = 1;
                    return values;
                })();

                LedIndicator.State = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "OFF"] = 0;
                    values[valuesById[1] = "ON"] = 1;
                    return values;
                })();

                return LedIndicator;
            })();

            control.LogMessage = (function() {

                function LogMessage(properties) {
                    this.entries = [];
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                LogMessage.prototype.contents = "";
                LogMessage.prototype.start = $util.Long ? $util.Long.fromBits(0,0,false) : 0;
                LogMessage.prototype.next = $util.Long ? $util.Long.fromBits(0,0,false) : 0;
                LogMessage.prototype.sort = 0;
                LogMessage.prototype.entries = $util.emptyArray;

                LogMessage.create = function create(properties) {
                    return new LogMessage(properties);
                };

                LogMessage.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.contents != null && Object.hasOwnProperty.call(message, "contents"))
                        writer.uint32(10).string(message.contents);
                    if (message.start != null && Object.hasOwnProperty.call(message, "start"))
                        writer.uint32(16).int64(message.start);
                    if (message.next != null && Object.hasOwnProperty.call(message, "next"))
                        writer.uint32(24).int64(message.next);
                    if (message.sort != null && Object.hasOwnProperty.call(message, "sort"))
                        writer.uint32(32).int32(message.sort);
                    if (message.entries != null && message.entries.length)
                        for (let i = 0; i < message.entries.length; ++i)
                            $root.android.emulation.control.LogcatEntry.encode(message.entries[i], writer.uint32(42).fork()).ldelim();
                    return writer;
                };

                LogMessage.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.LogMessage();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.contents = reader.string();
                                break;
                            }
                        case 2: {
                                message.start = reader.int64();
                                break;
                            }
                        case 3: {
                                message.next = reader.int64();
                                break;
                            }
                        case 4: {
                                message.sort = reader.int32();
                                break;
                            }
                        case 5: {
                                if (!(message.entries && message.entries.length))
                                    message.entries = [];
                                message.entries.push($root.android.emulation.control.LogcatEntry.decode(reader, reader.uint32()));
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                LogMessage.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.LogMessage";
                };

                LogMessage.LogType = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "Text"] = 0;
                    values[valuesById[1] = "Parsed"] = 1;
                    return values;
                })();

                return LogMessage;
            })();

            control.LogcatEntry = (function() {

                function LogcatEntry(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                LogcatEntry.prototype.timestamp = $util.Long ? $util.Long.fromBits(0,0,true) : 0;
                LogcatEntry.prototype.pid = 0;
                LogcatEntry.prototype.tid = 0;
                LogcatEntry.prototype.level = 0;
                LogcatEntry.prototype.tag = "";
                LogcatEntry.prototype.msg = "";

                LogcatEntry.create = function create(properties) {
                    return new LogcatEntry(properties);
                };

                LogcatEntry.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.timestamp != null && Object.hasOwnProperty.call(message, "timestamp"))
                        writer.uint32(8).uint64(message.timestamp);
                    if (message.pid != null && Object.hasOwnProperty.call(message, "pid"))
                        writer.uint32(16).uint32(message.pid);
                    if (message.tid != null && Object.hasOwnProperty.call(message, "tid"))
                        writer.uint32(24).uint32(message.tid);
                    if (message.level != null && Object.hasOwnProperty.call(message, "level"))
                        writer.uint32(32).int32(message.level);
                    if (message.tag != null && Object.hasOwnProperty.call(message, "tag"))
                        writer.uint32(42).string(message.tag);
                    if (message.msg != null && Object.hasOwnProperty.call(message, "msg"))
                        writer.uint32(50).string(message.msg);
                    return writer;
                };

                LogcatEntry.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.LogcatEntry();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.timestamp = reader.uint64();
                                break;
                            }
                        case 2: {
                                message.pid = reader.uint32();
                                break;
                            }
                        case 3: {
                                message.tid = reader.uint32();
                                break;
                            }
                        case 4: {
                                message.level = reader.int32();
                                break;
                            }
                        case 5: {
                                message.tag = reader.string();
                                break;
                            }
                        case 6: {
                                message.msg = reader.string();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                LogcatEntry.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.LogcatEntry";
                };

                LogcatEntry.LogLevel = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "UNKNOWN"] = 0;
                    values[valuesById[1] = "DEFAULT"] = 1;
                    values[valuesById[2] = "VERBOSE"] = 2;
                    values[valuesById[3] = "DEBUG"] = 3;
                    values[valuesById[4] = "INFO"] = 4;
                    values[valuesById[5] = "WARN"] = 5;
                    values[valuesById[6] = "ERR"] = 6;
                    values[valuesById[7] = "FATAL"] = 7;
                    values[valuesById[8] = "SILENT"] = 8;
                    return values;
                })();

                return LogcatEntry;
            })();

            control.VmConfiguration = (function() {

                function VmConfiguration(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                VmConfiguration.prototype.hypervisorType = 0;
                VmConfiguration.prototype.numberOfCpuCores = 0;
                VmConfiguration.prototype.ramSizeBytes = $util.Long ? $util.Long.fromBits(0,0,false) : 0;

                VmConfiguration.create = function create(properties) {
                    return new VmConfiguration(properties);
                };

                VmConfiguration.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.hypervisorType != null && Object.hasOwnProperty.call(message, "hypervisorType"))
                        writer.uint32(8).int32(message.hypervisorType);
                    if (message.numberOfCpuCores != null && Object.hasOwnProperty.call(message, "numberOfCpuCores"))
                        writer.uint32(16).int32(message.numberOfCpuCores);
                    if (message.ramSizeBytes != null && Object.hasOwnProperty.call(message, "ramSizeBytes"))
                        writer.uint32(24).int64(message.ramSizeBytes);
                    return writer;
                };

                VmConfiguration.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.VmConfiguration();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.hypervisorType = reader.int32();
                                break;
                            }
                        case 2: {
                                message.numberOfCpuCores = reader.int32();
                                break;
                            }
                        case 3: {
                                message.ramSizeBytes = reader.int64();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                VmConfiguration.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.VmConfiguration";
                };

                VmConfiguration.VmHypervisorType = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "UNKNOWN"] = 0;
                    values[valuesById[1] = "NONE"] = 1;
                    values[valuesById[2] = "KVM"] = 2;
                    values[valuesById[3] = "HAXM"] = 3;
                    values[valuesById[4] = "HVF"] = 4;
                    values[valuesById[5] = "WHPX"] = 5;
                    values[valuesById[6] = "AEHD"] = 6;
                    return values;
                })();

                return VmConfiguration;
            })();

            control.ClipData = (function() {

                function ClipData(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                ClipData.prototype.text = "";

                ClipData.create = function create(properties) {
                    return new ClipData(properties);
                };

                ClipData.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.text != null && Object.hasOwnProperty.call(message, "text"))
                        writer.uint32(10).string(message.text);
                    return writer;
                };

                ClipData.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.ClipData();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.text = reader.string();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                ClipData.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.ClipData";
                };

                return ClipData;
            })();

            control.Touch = (function() {

                function Touch(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                Touch.prototype.x = 0;
                Touch.prototype.y = 0;
                Touch.prototype.identifier = 0;
                Touch.prototype.pressure = 0;
                Touch.prototype.touchMajor = 0;
                Touch.prototype.touchMinor = 0;
                Touch.prototype.expiration = 0;
                Touch.prototype.orientation = 0;

                Touch.create = function create(properties) {
                    return new Touch(properties);
                };

                Touch.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.x != null && Object.hasOwnProperty.call(message, "x"))
                        writer.uint32(8).int32(message.x);
                    if (message.y != null && Object.hasOwnProperty.call(message, "y"))
                        writer.uint32(16).int32(message.y);
                    if (message.identifier != null && Object.hasOwnProperty.call(message, "identifier"))
                        writer.uint32(24).int32(message.identifier);
                    if (message.pressure != null && Object.hasOwnProperty.call(message, "pressure"))
                        writer.uint32(32).int32(message.pressure);
                    if (message.touchMajor != null && Object.hasOwnProperty.call(message, "touchMajor"))
                        writer.uint32(40).int32(message.touchMajor);
                    if (message.touchMinor != null && Object.hasOwnProperty.call(message, "touchMinor"))
                        writer.uint32(48).int32(message.touchMinor);
                    if (message.expiration != null && Object.hasOwnProperty.call(message, "expiration"))
                        writer.uint32(56).int32(message.expiration);
                    if (message.orientation != null && Object.hasOwnProperty.call(message, "orientation"))
                        writer.uint32(64).int32(message.orientation);
                    return writer;
                };

                Touch.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.Touch();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.x = reader.int32();
                                break;
                            }
                        case 2: {
                                message.y = reader.int32();
                                break;
                            }
                        case 3: {
                                message.identifier = reader.int32();
                                break;
                            }
                        case 4: {
                                message.pressure = reader.int32();
                                break;
                            }
                        case 5: {
                                message.touchMajor = reader.int32();
                                break;
                            }
                        case 6: {
                                message.touchMinor = reader.int32();
                                break;
                            }
                        case 7: {
                                message.expiration = reader.int32();
                                break;
                            }
                        case 8: {
                                message.orientation = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                Touch.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.Touch";
                };

                Touch.EventExpiration = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "EVENT_EXPIRATION_UNSPECIFIED"] = 0;
                    values[valuesById[1] = "NEVER_EXPIRE"] = 1;
                    return values;
                })();

                return Touch;
            })();

            control.Pen = (function() {

                function Pen(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                Pen.prototype.location = null;
                Pen.prototype.buttonPressed = false;
                Pen.prototype.rubberPointer = false;

                Pen.create = function create(properties) {
                    return new Pen(properties);
                };

                Pen.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.location != null && Object.hasOwnProperty.call(message, "location"))
                        $root.android.emulation.control.Touch.encode(message.location, writer.uint32(10).fork()).ldelim();
                    if (message.buttonPressed != null && Object.hasOwnProperty.call(message, "buttonPressed"))
                        writer.uint32(16).bool(message.buttonPressed);
                    if (message.rubberPointer != null && Object.hasOwnProperty.call(message, "rubberPointer"))
                        writer.uint32(24).bool(message.rubberPointer);
                    return writer;
                };

                Pen.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.Pen();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.location = $root.android.emulation.control.Touch.decode(reader, reader.uint32());
                                break;
                            }
                        case 2: {
                                message.buttonPressed = reader.bool();
                                break;
                            }
                        case 3: {
                                message.rubberPointer = reader.bool();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                Pen.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.Pen";
                };

                return Pen;
            })();

            control.TouchEvent = (function() {

                function TouchEvent(properties) {
                    this.touches = [];
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                TouchEvent.prototype.touches = $util.emptyArray;
                TouchEvent.prototype.display = 0;

                TouchEvent.create = function create(properties) {
                    return new TouchEvent(properties);
                };

                TouchEvent.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.touches != null && message.touches.length)
                        for (let i = 0; i < message.touches.length; ++i)
                            $root.android.emulation.control.Touch.encode(message.touches[i], writer.uint32(10).fork()).ldelim();
                    if (message.display != null && Object.hasOwnProperty.call(message, "display"))
                        writer.uint32(16).int32(message.display);
                    return writer;
                };

                TouchEvent.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.TouchEvent();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                if (!(message.touches && message.touches.length))
                                    message.touches = [];
                                message.touches.push($root.android.emulation.control.Touch.decode(reader, reader.uint32()));
                                break;
                            }
                        case 2: {
                                message.display = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                TouchEvent.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.TouchEvent";
                };

                return TouchEvent;
            })();

            control.TouchpadEvent = (function() {

                function TouchpadEvent(properties) {
                    this.touches = [];
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                TouchpadEvent.prototype.touches = $util.emptyArray;
                TouchpadEvent.prototype.touchpad = 0;

                TouchpadEvent.create = function create(properties) {
                    return new TouchpadEvent(properties);
                };

                TouchpadEvent.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.touches != null && message.touches.length)
                        for (let i = 0; i < message.touches.length; ++i)
                            $root.android.emulation.control.Touch.encode(message.touches[i], writer.uint32(10).fork()).ldelim();
                    if (message.touchpad != null && Object.hasOwnProperty.call(message, "touchpad"))
                        writer.uint32(16).int32(message.touchpad);
                    return writer;
                };

                TouchpadEvent.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.TouchpadEvent();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                if (!(message.touches && message.touches.length))
                                    message.touches = [];
                                message.touches.push($root.android.emulation.control.Touch.decode(reader, reader.uint32()));
                                break;
                            }
                        case 2: {
                                message.touchpad = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                TouchpadEvent.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.TouchpadEvent";
                };

                return TouchpadEvent;
            })();

            control.PenEvent = (function() {

                function PenEvent(properties) {
                    this.events = [];
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                PenEvent.prototype.events = $util.emptyArray;
                PenEvent.prototype.display = 0;

                PenEvent.create = function create(properties) {
                    return new PenEvent(properties);
                };

                PenEvent.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.events != null && message.events.length)
                        for (let i = 0; i < message.events.length; ++i)
                            $root.android.emulation.control.Pen.encode(message.events[i], writer.uint32(10).fork()).ldelim();
                    if (message.display != null && Object.hasOwnProperty.call(message, "display"))
                        writer.uint32(16).int32(message.display);
                    return writer;
                };

                PenEvent.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.PenEvent();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                if (!(message.events && message.events.length))
                                    message.events = [];
                                message.events.push($root.android.emulation.control.Pen.decode(reader, reader.uint32()));
                                break;
                            }
                        case 2: {
                                message.display = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                PenEvent.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.PenEvent";
                };

                return PenEvent;
            })();

            control.MouseEvent = (function() {

                function MouseEvent(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                MouseEvent.prototype.x = 0;
                MouseEvent.prototype.y = 0;
                MouseEvent.prototype.buttons = 0;
                MouseEvent.prototype.display = 0;

                MouseEvent.create = function create(properties) {
                    return new MouseEvent(properties);
                };

                MouseEvent.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.x != null && Object.hasOwnProperty.call(message, "x"))
                        writer.uint32(8).int32(message.x);
                    if (message.y != null && Object.hasOwnProperty.call(message, "y"))
                        writer.uint32(16).int32(message.y);
                    if (message.buttons != null && Object.hasOwnProperty.call(message, "buttons"))
                        writer.uint32(24).int32(message.buttons);
                    if (message.display != null && Object.hasOwnProperty.call(message, "display"))
                        writer.uint32(32).int32(message.display);
                    return writer;
                };

                MouseEvent.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.MouseEvent();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.x = reader.int32();
                                break;
                            }
                        case 2: {
                                message.y = reader.int32();
                                break;
                            }
                        case 3: {
                                message.buttons = reader.int32();
                                break;
                            }
                        case 4: {
                                message.display = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                MouseEvent.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.MouseEvent";
                };

                return MouseEvent;
            })();

            control.WheelEvent = (function() {

                function WheelEvent(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                WheelEvent.prototype.dx = 0;
                WheelEvent.prototype.dy = 0;
                WheelEvent.prototype.display = 0;

                WheelEvent.create = function create(properties) {
                    return new WheelEvent(properties);
                };

                WheelEvent.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.dx != null && Object.hasOwnProperty.call(message, "dx"))
                        writer.uint32(8).int32(message.dx);
                    if (message.dy != null && Object.hasOwnProperty.call(message, "dy"))
                        writer.uint32(16).int32(message.dy);
                    if (message.display != null && Object.hasOwnProperty.call(message, "display"))
                        writer.uint32(24).int32(message.display);
                    return writer;
                };

                WheelEvent.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.WheelEvent();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.dx = reader.int32();
                                break;
                            }
                        case 2: {
                                message.dy = reader.int32();
                                break;
                            }
                        case 3: {
                                message.display = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                WheelEvent.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.WheelEvent";
                };

                return WheelEvent;
            })();

            control.KeyboardEvent = (function() {

                function KeyboardEvent(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                KeyboardEvent.prototype.codeType = 0;
                KeyboardEvent.prototype.eventType = 0;
                KeyboardEvent.prototype.keyCode = 0;
                KeyboardEvent.prototype.key = "";
                KeyboardEvent.prototype.text = "";

                KeyboardEvent.create = function create(properties) {
                    return new KeyboardEvent(properties);
                };

                KeyboardEvent.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.codeType != null && Object.hasOwnProperty.call(message, "codeType"))
                        writer.uint32(8).int32(message.codeType);
                    if (message.eventType != null && Object.hasOwnProperty.call(message, "eventType"))
                        writer.uint32(16).int32(message.eventType);
                    if (message.keyCode != null && Object.hasOwnProperty.call(message, "keyCode"))
                        writer.uint32(24).int32(message.keyCode);
                    if (message.key != null && Object.hasOwnProperty.call(message, "key"))
                        writer.uint32(34).string(message.key);
                    if (message.text != null && Object.hasOwnProperty.call(message, "text"))
                        writer.uint32(42).string(message.text);
                    return writer;
                };

                KeyboardEvent.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.KeyboardEvent();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.codeType = reader.int32();
                                break;
                            }
                        case 2: {
                                message.eventType = reader.int32();
                                break;
                            }
                        case 3: {
                                message.keyCode = reader.int32();
                                break;
                            }
                        case 4: {
                                message.key = reader.string();
                                break;
                            }
                        case 5: {
                                message.text = reader.string();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                KeyboardEvent.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.KeyboardEvent";
                };

                KeyboardEvent.KeyCodeType = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "Usb"] = 0;
                    values[valuesById[1] = "Evdev"] = 1;
                    values[valuesById[2] = "XKB"] = 2;
                    values[valuesById[3] = "Win"] = 3;
                    values[valuesById[4] = "Mac"] = 4;
                    return values;
                })();

                KeyboardEvent.KeyEventType = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "keydown"] = 0;
                    values[valuesById[1] = "keyup"] = 1;
                    values[valuesById[2] = "keypress"] = 2;
                    return values;
                })();

                return KeyboardEvent;
            })();

            control.XrCommand = (function() {

                function XrCommand(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                XrCommand.prototype.action = 0;

                XrCommand.create = function create(properties) {
                    return new XrCommand(properties);
                };

                XrCommand.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.action != null && Object.hasOwnProperty.call(message, "action"))
                        writer.uint32(8).int32(message.action);
                    return writer;
                };

                XrCommand.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.XrCommand();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.action = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                XrCommand.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.XrCommand";
                };

                XrCommand.Action = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "RECENTER"] = 0;
                    return values;
                })();

                return XrCommand;
            })();

            control.InputEvent = (function() {

                function InputEvent(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                InputEvent.prototype.keyEvent = null;
                InputEvent.prototype.touchEvent = null;
                InputEvent.prototype.mouseEvent = null;
                InputEvent.prototype.androidEvent = null;
                InputEvent.prototype.penEvent = null;
                InputEvent.prototype.wheelEvent = null;
                InputEvent.prototype.xrHandEvent = null;
                InputEvent.prototype.xrEyeEvent = null;
                InputEvent.prototype.xrCommand = null;
                InputEvent.prototype.xrHeadRotationEvent = null;
                InputEvent.prototype.xrHeadMovementEvent = null;
                InputEvent.prototype.xrHeadAngularVelocityEvent = null;
                InputEvent.prototype.xrHeadVelocityEvent = null;
                InputEvent.prototype.touchpadEvent = null;

                let $oneOfFields;

                Object.defineProperty(InputEvent.prototype, "type", {
                    get: $util.oneOfGetter($oneOfFields = ["keyEvent", "touchEvent", "mouseEvent", "androidEvent", "penEvent", "wheelEvent", "xrHandEvent", "xrEyeEvent", "xrCommand", "xrHeadRotationEvent", "xrHeadMovementEvent", "xrHeadAngularVelocityEvent", "xrHeadVelocityEvent", "touchpadEvent"]),
                    set: $util.oneOfSetter($oneOfFields)
                });

                InputEvent.create = function create(properties) {
                    return new InputEvent(properties);
                };

                InputEvent.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.keyEvent != null && Object.hasOwnProperty.call(message, "keyEvent"))
                        $root.android.emulation.control.KeyboardEvent.encode(message.keyEvent, writer.uint32(10).fork()).ldelim();
                    if (message.touchEvent != null && Object.hasOwnProperty.call(message, "touchEvent"))
                        $root.android.emulation.control.TouchEvent.encode(message.touchEvent, writer.uint32(18).fork()).ldelim();
                    if (message.mouseEvent != null && Object.hasOwnProperty.call(message, "mouseEvent"))
                        $root.android.emulation.control.MouseEvent.encode(message.mouseEvent, writer.uint32(26).fork()).ldelim();
                    if (message.androidEvent != null && Object.hasOwnProperty.call(message, "androidEvent"))
                        $root.android.emulation.control.AndroidEvent.encode(message.androidEvent, writer.uint32(34).fork()).ldelim();
                    if (message.penEvent != null && Object.hasOwnProperty.call(message, "penEvent"))
                        $root.android.emulation.control.PenEvent.encode(message.penEvent, writer.uint32(42).fork()).ldelim();
                    if (message.wheelEvent != null && Object.hasOwnProperty.call(message, "wheelEvent"))
                        $root.android.emulation.control.WheelEvent.encode(message.wheelEvent, writer.uint32(50).fork()).ldelim();
                    if (message.xrHandEvent != null && Object.hasOwnProperty.call(message, "xrHandEvent"))
                        $root.android.emulation.control.MouseEvent.encode(message.xrHandEvent, writer.uint32(58).fork()).ldelim();
                    if (message.xrEyeEvent != null && Object.hasOwnProperty.call(message, "xrEyeEvent"))
                        $root.android.emulation.control.MouseEvent.encode(message.xrEyeEvent, writer.uint32(66).fork()).ldelim();
                    if (message.xrCommand != null && Object.hasOwnProperty.call(message, "xrCommand"))
                        $root.android.emulation.control.XrCommand.encode(message.xrCommand, writer.uint32(74).fork()).ldelim();
                    if (message.xrHeadRotationEvent != null && Object.hasOwnProperty.call(message, "xrHeadRotationEvent"))
                        $root.android.emulation.control.RotationRadian.encode(message.xrHeadRotationEvent, writer.uint32(82).fork()).ldelim();
                    if (message.xrHeadMovementEvent != null && Object.hasOwnProperty.call(message, "xrHeadMovementEvent"))
                        $root.android.emulation.control.Translation.encode(message.xrHeadMovementEvent, writer.uint32(90).fork()).ldelim();
                    if (message.xrHeadAngularVelocityEvent != null && Object.hasOwnProperty.call(message, "xrHeadAngularVelocityEvent"))
                        $root.android.emulation.control.AngularVelocity.encode(message.xrHeadAngularVelocityEvent, writer.uint32(98).fork()).ldelim();
                    if (message.xrHeadVelocityEvent != null && Object.hasOwnProperty.call(message, "xrHeadVelocityEvent"))
                        $root.android.emulation.control.Velocity.encode(message.xrHeadVelocityEvent, writer.uint32(106).fork()).ldelim();
                    if (message.touchpadEvent != null && Object.hasOwnProperty.call(message, "touchpadEvent"))
                        $root.android.emulation.control.TouchpadEvent.encode(message.touchpadEvent, writer.uint32(114).fork()).ldelim();
                    return writer;
                };

                InputEvent.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.InputEvent();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.keyEvent = $root.android.emulation.control.KeyboardEvent.decode(reader, reader.uint32());
                                break;
                            }
                        case 2: {
                                message.touchEvent = $root.android.emulation.control.TouchEvent.decode(reader, reader.uint32());
                                break;
                            }
                        case 3: {
                                message.mouseEvent = $root.android.emulation.control.MouseEvent.decode(reader, reader.uint32());
                                break;
                            }
                        case 4: {
                                message.androidEvent = $root.android.emulation.control.AndroidEvent.decode(reader, reader.uint32());
                                break;
                            }
                        case 5: {
                                message.penEvent = $root.android.emulation.control.PenEvent.decode(reader, reader.uint32());
                                break;
                            }
                        case 6: {
                                message.wheelEvent = $root.android.emulation.control.WheelEvent.decode(reader, reader.uint32());
                                break;
                            }
                        case 7: {
                                message.xrHandEvent = $root.android.emulation.control.MouseEvent.decode(reader, reader.uint32());
                                break;
                            }
                        case 8: {
                                message.xrEyeEvent = $root.android.emulation.control.MouseEvent.decode(reader, reader.uint32());
                                break;
                            }
                        case 9: {
                                message.xrCommand = $root.android.emulation.control.XrCommand.decode(reader, reader.uint32());
                                break;
                            }
                        case 10: {
                                message.xrHeadRotationEvent = $root.android.emulation.control.RotationRadian.decode(reader, reader.uint32());
                                break;
                            }
                        case 11: {
                                message.xrHeadMovementEvent = $root.android.emulation.control.Translation.decode(reader, reader.uint32());
                                break;
                            }
                        case 12: {
                                message.xrHeadAngularVelocityEvent = $root.android.emulation.control.AngularVelocity.decode(reader, reader.uint32());
                                break;
                            }
                        case 13: {
                                message.xrHeadVelocityEvent = $root.android.emulation.control.Velocity.decode(reader, reader.uint32());
                                break;
                            }
                        case 14: {
                                message.touchpadEvent = $root.android.emulation.control.TouchpadEvent.decode(reader, reader.uint32());
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                InputEvent.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.InputEvent";
                };

                return InputEvent;
            })();

            control.AndroidEvent = (function() {

                function AndroidEvent(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                AndroidEvent.prototype.type = 0;
                AndroidEvent.prototype.code = 0;
                AndroidEvent.prototype.value = 0;
                AndroidEvent.prototype.display = 0;

                AndroidEvent.create = function create(properties) {
                    return new AndroidEvent(properties);
                };

                AndroidEvent.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.type != null && Object.hasOwnProperty.call(message, "type"))
                        writer.uint32(8).int32(message.type);
                    if (message.code != null && Object.hasOwnProperty.call(message, "code"))
                        writer.uint32(16).int32(message.code);
                    if (message.value != null && Object.hasOwnProperty.call(message, "value"))
                        writer.uint32(24).int32(message.value);
                    if (message.display != null && Object.hasOwnProperty.call(message, "display"))
                        writer.uint32(32).int32(message.display);
                    return writer;
                };

                AndroidEvent.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.AndroidEvent();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.type = reader.int32();
                                break;
                            }
                        case 2: {
                                message.code = reader.int32();
                                break;
                            }
                        case 3: {
                                message.value = reader.int32();
                                break;
                            }
                        case 4: {
                                message.display = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                AndroidEvent.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.AndroidEvent";
                };

                return AndroidEvent;
            })();

            control.Fingerprint = (function() {

                function Fingerprint(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                Fingerprint.prototype.isTouching = false;
                Fingerprint.prototype.touchId = 0;

                Fingerprint.create = function create(properties) {
                    return new Fingerprint(properties);
                };

                Fingerprint.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.isTouching != null && Object.hasOwnProperty.call(message, "isTouching"))
                        writer.uint32(8).bool(message.isTouching);
                    if (message.touchId != null && Object.hasOwnProperty.call(message, "touchId"))
                        writer.uint32(16).int32(message.touchId);
                    return writer;
                };

                Fingerprint.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.Fingerprint();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.isTouching = reader.bool();
                                break;
                            }
                        case 2: {
                                message.touchId = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                Fingerprint.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.Fingerprint";
                };

                return Fingerprint;
            })();

            control.GpsState = (function() {

                function GpsState(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                GpsState.prototype.passiveUpdate = false;
                GpsState.prototype.latitude = 0;
                GpsState.prototype.longitude = 0;
                GpsState.prototype.speed = 0;
                GpsState.prototype.bearing = 0;
                GpsState.prototype.altitude = 0;
                GpsState.prototype.satellites = 0;

                GpsState.create = function create(properties) {
                    return new GpsState(properties);
                };

                GpsState.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.passiveUpdate != null && Object.hasOwnProperty.call(message, "passiveUpdate"))
                        writer.uint32(8).bool(message.passiveUpdate);
                    if (message.latitude != null && Object.hasOwnProperty.call(message, "latitude"))
                        writer.uint32(17).double(message.latitude);
                    if (message.longitude != null && Object.hasOwnProperty.call(message, "longitude"))
                        writer.uint32(25).double(message.longitude);
                    if (message.speed != null && Object.hasOwnProperty.call(message, "speed"))
                        writer.uint32(33).double(message.speed);
                    if (message.bearing != null && Object.hasOwnProperty.call(message, "bearing"))
                        writer.uint32(41).double(message.bearing);
                    if (message.altitude != null && Object.hasOwnProperty.call(message, "altitude"))
                        writer.uint32(49).double(message.altitude);
                    if (message.satellites != null && Object.hasOwnProperty.call(message, "satellites"))
                        writer.uint32(56).int32(message.satellites);
                    return writer;
                };

                GpsState.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.GpsState();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.passiveUpdate = reader.bool();
                                break;
                            }
                        case 2: {
                                message.latitude = reader.double();
                                break;
                            }
                        case 3: {
                                message.longitude = reader.double();
                                break;
                            }
                        case 4: {
                                message.speed = reader.double();
                                break;
                            }
                        case 5: {
                                message.bearing = reader.double();
                                break;
                            }
                        case 6: {
                                message.altitude = reader.double();
                                break;
                            }
                        case 7: {
                                message.satellites = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                GpsState.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.GpsState";
                };

                return GpsState;
            })();

            control.BatteryState = (function() {

                function BatteryState(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                BatteryState.prototype.hasBattery = false;
                BatteryState.prototype.isPresent = false;
                BatteryState.prototype.charger = 0;
                BatteryState.prototype.chargeLevel = 0;
                BatteryState.prototype.health = 0;
                BatteryState.prototype.status = 0;

                BatteryState.create = function create(properties) {
                    return new BatteryState(properties);
                };

                BatteryState.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.hasBattery != null && Object.hasOwnProperty.call(message, "hasBattery"))
                        writer.uint32(8).bool(message.hasBattery);
                    if (message.isPresent != null && Object.hasOwnProperty.call(message, "isPresent"))
                        writer.uint32(16).bool(message.isPresent);
                    if (message.charger != null && Object.hasOwnProperty.call(message, "charger"))
                        writer.uint32(24).int32(message.charger);
                    if (message.chargeLevel != null && Object.hasOwnProperty.call(message, "chargeLevel"))
                        writer.uint32(32).int32(message.chargeLevel);
                    if (message.health != null && Object.hasOwnProperty.call(message, "health"))
                        writer.uint32(40).int32(message.health);
                    if (message.status != null && Object.hasOwnProperty.call(message, "status"))
                        writer.uint32(48).int32(message.status);
                    return writer;
                };

                BatteryState.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.BatteryState();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.hasBattery = reader.bool();
                                break;
                            }
                        case 2: {
                                message.isPresent = reader.bool();
                                break;
                            }
                        case 3: {
                                message.charger = reader.int32();
                                break;
                            }
                        case 4: {
                                message.chargeLevel = reader.int32();
                                break;
                            }
                        case 5: {
                                message.health = reader.int32();
                                break;
                            }
                        case 6: {
                                message.status = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                BatteryState.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.BatteryState";
                };

                BatteryState.BatteryStatus = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "UNKNOWN"] = 0;
                    values[valuesById[1] = "CHARGING"] = 1;
                    values[valuesById[2] = "DISCHARGING"] = 2;
                    values[valuesById[3] = "NOT_CHARGING"] = 3;
                    values[valuesById[4] = "FULL"] = 4;
                    return values;
                })();

                BatteryState.BatteryCharger = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "NONE"] = 0;
                    values[valuesById[1] = "AC"] = 1;
                    values[valuesById[2] = "USB"] = 2;
                    values[valuesById[3] = "WIRELESS"] = 3;
                    return values;
                })();

                BatteryState.BatteryHealth = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "GOOD"] = 0;
                    values[valuesById[1] = "FAILED"] = 1;
                    values[valuesById[2] = "DEAD"] = 2;
                    values[valuesById[3] = "OVERVOLTAGE"] = 3;
                    values[valuesById[4] = "OVERHEATED"] = 4;
                    return values;
                })();

                return BatteryState;
            })();

            control.ImageTransport = (function() {

                function ImageTransport(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                ImageTransport.prototype.channel = 0;
                ImageTransport.prototype.handle = "";

                ImageTransport.create = function create(properties) {
                    return new ImageTransport(properties);
                };

                ImageTransport.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.channel != null && Object.hasOwnProperty.call(message, "channel"))
                        writer.uint32(8).int32(message.channel);
                    if (message.handle != null && Object.hasOwnProperty.call(message, "handle"))
                        writer.uint32(18).string(message.handle);
                    return writer;
                };

                ImageTransport.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.ImageTransport();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.channel = reader.int32();
                                break;
                            }
                        case 2: {
                                message.handle = reader.string();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                ImageTransport.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.ImageTransport";
                };

                ImageTransport.TransportChannel = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "TRANSPORT_CHANNEL_UNSPECIFIED"] = 0;
                    values[valuesById[1] = "MMAP"] = 1;
                    return values;
                })();

                return ImageTransport;
            })();

            control.FoldedDisplay = (function() {

                function FoldedDisplay(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                FoldedDisplay.prototype.width = 0;
                FoldedDisplay.prototype.height = 0;
                FoldedDisplay.prototype.xOffset = 0;
                FoldedDisplay.prototype.yOffset = 0;

                FoldedDisplay.create = function create(properties) {
                    return new FoldedDisplay(properties);
                };

                FoldedDisplay.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.width != null && Object.hasOwnProperty.call(message, "width"))
                        writer.uint32(8).uint32(message.width);
                    if (message.height != null && Object.hasOwnProperty.call(message, "height"))
                        writer.uint32(16).uint32(message.height);
                    if (message.xOffset != null && Object.hasOwnProperty.call(message, "xOffset"))
                        writer.uint32(24).uint32(message.xOffset);
                    if (message.yOffset != null && Object.hasOwnProperty.call(message, "yOffset"))
                        writer.uint32(32).uint32(message.yOffset);
                    return writer;
                };

                FoldedDisplay.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.FoldedDisplay();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.width = reader.uint32();
                                break;
                            }
                        case 2: {
                                message.height = reader.uint32();
                                break;
                            }
                        case 3: {
                                message.xOffset = reader.uint32();
                                break;
                            }
                        case 4: {
                                message.yOffset = reader.uint32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                FoldedDisplay.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.FoldedDisplay";
                };

                return FoldedDisplay;
            })();

            control.ImageFormat = (function() {

                function ImageFormat(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                ImageFormat.prototype.format = 0;
                ImageFormat.prototype.rotation = null;
                ImageFormat.prototype.width = 0;
                ImageFormat.prototype.height = 0;
                ImageFormat.prototype.display = 0;
                ImageFormat.prototype.transport = null;
                ImageFormat.prototype.foldedDisplay = null;
                ImageFormat.prototype.displayMode = 0;

                ImageFormat.create = function create(properties) {
                    return new ImageFormat(properties);
                };

                ImageFormat.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.format != null && Object.hasOwnProperty.call(message, "format"))
                        writer.uint32(8).int32(message.format);
                    if (message.rotation != null && Object.hasOwnProperty.call(message, "rotation"))
                        $root.android.emulation.control.Rotation.encode(message.rotation, writer.uint32(18).fork()).ldelim();
                    if (message.width != null && Object.hasOwnProperty.call(message, "width"))
                        writer.uint32(24).uint32(message.width);
                    if (message.height != null && Object.hasOwnProperty.call(message, "height"))
                        writer.uint32(32).uint32(message.height);
                    if (message.display != null && Object.hasOwnProperty.call(message, "display"))
                        writer.uint32(40).uint32(message.display);
                    if (message.transport != null && Object.hasOwnProperty.call(message, "transport"))
                        $root.android.emulation.control.ImageTransport.encode(message.transport, writer.uint32(50).fork()).ldelim();
                    if (message.foldedDisplay != null && Object.hasOwnProperty.call(message, "foldedDisplay"))
                        $root.android.emulation.control.FoldedDisplay.encode(message.foldedDisplay, writer.uint32(58).fork()).ldelim();
                    if (message.displayMode != null && Object.hasOwnProperty.call(message, "displayMode"))
                        writer.uint32(64).int32(message.displayMode);
                    return writer;
                };

                ImageFormat.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.ImageFormat();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.format = reader.int32();
                                break;
                            }
                        case 2: {
                                message.rotation = $root.android.emulation.control.Rotation.decode(reader, reader.uint32());
                                break;
                            }
                        case 3: {
                                message.width = reader.uint32();
                                break;
                            }
                        case 4: {
                                message.height = reader.uint32();
                                break;
                            }
                        case 5: {
                                message.display = reader.uint32();
                                break;
                            }
                        case 6: {
                                message.transport = $root.android.emulation.control.ImageTransport.decode(reader, reader.uint32());
                                break;
                            }
                        case 7: {
                                message.foldedDisplay = $root.android.emulation.control.FoldedDisplay.decode(reader, reader.uint32());
                                break;
                            }
                        case 8: {
                                message.displayMode = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                ImageFormat.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.ImageFormat";
                };

                ImageFormat.ImgFormat = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "PNG"] = 0;
                    values[valuesById[1] = "RGBA8888"] = 1;
                    values[valuesById[2] = "RGB888"] = 2;
                    return values;
                })();

                return ImageFormat;
            })();

            control.Image = (function() {

                function Image(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                Image.prototype.format = null;
                Image.prototype.width = 0;
                Image.prototype.height = 0;
                Image.prototype.image = $util.newBuffer([]);
                Image.prototype.seq = 0;
                Image.prototype.timestampUs = $util.Long ? $util.Long.fromBits(0,0,true) : 0;

                Image.create = function create(properties) {
                    return new Image(properties);
                };

                Image.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.format != null && Object.hasOwnProperty.call(message, "format"))
                        $root.android.emulation.control.ImageFormat.encode(message.format, writer.uint32(10).fork()).ldelim();
                    if (message.width != null && Object.hasOwnProperty.call(message, "width"))
                        writer.uint32(16).uint32(message.width);
                    if (message.height != null && Object.hasOwnProperty.call(message, "height"))
                        writer.uint32(24).uint32(message.height);
                    if (message.image != null && Object.hasOwnProperty.call(message, "image"))
                        writer.uint32(34).bytes(message.image);
                    if (message.seq != null && Object.hasOwnProperty.call(message, "seq"))
                        writer.uint32(40).uint32(message.seq);
                    if (message.timestampUs != null && Object.hasOwnProperty.call(message, "timestampUs"))
                        writer.uint32(48).uint64(message.timestampUs);
                    return writer;
                };

                Image.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.Image();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.format = $root.android.emulation.control.ImageFormat.decode(reader, reader.uint32());
                                break;
                            }
                        case 2: {
                                message.width = reader.uint32();
                                break;
                            }
                        case 3: {
                                message.height = reader.uint32();
                                break;
                            }
                        case 4: {
                                message.image = reader.bytes();
                                break;
                            }
                        case 5: {
                                message.seq = reader.uint32();
                                break;
                            }
                        case 6: {
                                message.timestampUs = reader.uint64();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                Image.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.Image";
                };

                return Image;
            })();

            control.Rotation = (function() {

                function Rotation(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                Rotation.prototype.rotation = 0;
                Rotation.prototype.xAxis = 0;
                Rotation.prototype.yAxis = 0;
                Rotation.prototype.zAxis = 0;

                Rotation.create = function create(properties) {
                    return new Rotation(properties);
                };

                Rotation.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.rotation != null && Object.hasOwnProperty.call(message, "rotation"))
                        writer.uint32(8).int32(message.rotation);
                    if (message.xAxis != null && Object.hasOwnProperty.call(message, "xAxis"))
                        writer.uint32(17).double(message.xAxis);
                    if (message.yAxis != null && Object.hasOwnProperty.call(message, "yAxis"))
                        writer.uint32(25).double(message.yAxis);
                    if (message.zAxis != null && Object.hasOwnProperty.call(message, "zAxis"))
                        writer.uint32(33).double(message.zAxis);
                    return writer;
                };

                Rotation.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.Rotation();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.rotation = reader.int32();
                                break;
                            }
                        case 2: {
                                message.xAxis = reader.double();
                                break;
                            }
                        case 3: {
                                message.yAxis = reader.double();
                                break;
                            }
                        case 4: {
                                message.zAxis = reader.double();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                Rotation.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.Rotation";
                };

                Rotation.SkinRotation = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "PORTRAIT"] = 0;
                    values[valuesById[1] = "LANDSCAPE"] = 1;
                    values[valuesById[2] = "REVERSE_PORTRAIT"] = 2;
                    values[valuesById[3] = "REVERSE_LANDSCAPE"] = 3;
                    return values;
                })();

                return Rotation;
            })();

            control.PhoneCall = (function() {

                function PhoneCall(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                PhoneCall.prototype.operation = 0;
                PhoneCall.prototype.number = "";

                PhoneCall.create = function create(properties) {
                    return new PhoneCall(properties);
                };

                PhoneCall.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.operation != null && Object.hasOwnProperty.call(message, "operation"))
                        writer.uint32(8).int32(message.operation);
                    if (message.number != null && Object.hasOwnProperty.call(message, "number"))
                        writer.uint32(18).string(message.number);
                    return writer;
                };

                PhoneCall.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.PhoneCall();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.operation = reader.int32();
                                break;
                            }
                        case 2: {
                                message.number = reader.string();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                PhoneCall.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.PhoneCall";
                };

                PhoneCall.Operation = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "InitCall"] = 0;
                    values[valuesById[1] = "AcceptCall"] = 1;
                    values[valuesById[2] = "RejectCallExplicit"] = 2;
                    values[valuesById[3] = "RejectCallBusy"] = 3;
                    values[valuesById[4] = "DisconnectCall"] = 4;
                    values[valuesById[5] = "PlaceCallOnHold"] = 5;
                    values[valuesById[6] = "TakeCallOffHold"] = 6;
                    return values;
                })();

                return PhoneCall;
            })();

            control.PhoneResponse = (function() {

                function PhoneResponse(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                PhoneResponse.prototype.response = 0;

                PhoneResponse.create = function create(properties) {
                    return new PhoneResponse(properties);
                };

                PhoneResponse.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.response != null && Object.hasOwnProperty.call(message, "response"))
                        writer.uint32(8).int32(message.response);
                    return writer;
                };

                PhoneResponse.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.PhoneResponse();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.response = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                PhoneResponse.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.PhoneResponse";
                };

                PhoneResponse.Response = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "OK"] = 0;
                    values[valuesById[1] = "BadOperation"] = 1;
                    values[valuesById[2] = "BadNumber"] = 2;
                    values[valuesById[3] = "InvalidAction"] = 3;
                    values[valuesById[4] = "ActionFailed"] = 4;
                    values[valuesById[5] = "RadioOff"] = 5;
                    return values;
                })();

                return PhoneResponse;
            })();

            control.Entry = (function() {

                function Entry(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                Entry.prototype.key = "";
                Entry.prototype.value = "";

                Entry.create = function create(properties) {
                    return new Entry(properties);
                };

                Entry.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.key != null && Object.hasOwnProperty.call(message, "key"))
                        writer.uint32(10).string(message.key);
                    if (message.value != null && Object.hasOwnProperty.call(message, "value"))
                        writer.uint32(18).string(message.value);
                    return writer;
                };

                Entry.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.Entry();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.key = reader.string();
                                break;
                            }
                        case 2: {
                                message.value = reader.string();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                Entry.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.Entry";
                };

                return Entry;
            })();

            control.EntryList = (function() {

                function EntryList(properties) {
                    this.entry = [];
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                EntryList.prototype.entry = $util.emptyArray;

                EntryList.create = function create(properties) {
                    return new EntryList(properties);
                };

                EntryList.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.entry != null && message.entry.length)
                        for (let i = 0; i < message.entry.length; ++i)
                            $root.android.emulation.control.Entry.encode(message.entry[i], writer.uint32(10).fork()).ldelim();
                    return writer;
                };

                EntryList.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.EntryList();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                if (!(message.entry && message.entry.length))
                                    message.entry = [];
                                message.entry.push($root.android.emulation.control.Entry.decode(reader, reader.uint32()));
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                EntryList.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.EntryList";
                };

                return EntryList;
            })();

            control.EmulatorStatus = (function() {

                function EmulatorStatus(properties) {
                    this.guestConfig = {};
                    this.platformConfig = {};
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                EmulatorStatus.prototype.version = "";
                EmulatorStatus.prototype.uptime = $util.Long ? $util.Long.fromBits(0,0,true) : 0;
                EmulatorStatus.prototype.booted = false;
                EmulatorStatus.prototype.vmConfig = null;
                EmulatorStatus.prototype.hardwareConfig = null;
                EmulatorStatus.prototype.heartbeat = $util.Long ? $util.Long.fromBits(0,0,true) : 0;
                EmulatorStatus.prototype.guestConfig = $util.emptyObject;
                EmulatorStatus.prototype.platformConfig = $util.emptyObject;

                EmulatorStatus.create = function create(properties) {
                    return new EmulatorStatus(properties);
                };

                EmulatorStatus.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.version != null && Object.hasOwnProperty.call(message, "version"))
                        writer.uint32(10).string(message.version);
                    if (message.uptime != null && Object.hasOwnProperty.call(message, "uptime"))
                        writer.uint32(16).uint64(message.uptime);
                    if (message.booted != null && Object.hasOwnProperty.call(message, "booted"))
                        writer.uint32(24).bool(message.booted);
                    if (message.vmConfig != null && Object.hasOwnProperty.call(message, "vmConfig"))
                        $root.android.emulation.control.VmConfiguration.encode(message.vmConfig, writer.uint32(34).fork()).ldelim();
                    if (message.hardwareConfig != null && Object.hasOwnProperty.call(message, "hardwareConfig"))
                        $root.android.emulation.control.EntryList.encode(message.hardwareConfig, writer.uint32(42).fork()).ldelim();
                    if (message.heartbeat != null && Object.hasOwnProperty.call(message, "heartbeat"))
                        writer.uint32(48).uint64(message.heartbeat);
                    if (message.guestConfig != null && Object.hasOwnProperty.call(message, "guestConfig"))
                        for (let keys = Object.keys(message.guestConfig), i = 0; i < keys.length; ++i)
                            writer.uint32(58).fork().uint32(10).string(keys[i]).uint32(18).string(message.guestConfig[keys[i]]).ldelim();
                    if (message.platformConfig != null && Object.hasOwnProperty.call(message, "platformConfig"))
                        for (let keys = Object.keys(message.platformConfig), i = 0; i < keys.length; ++i)
                            writer.uint32(66).fork().uint32(10).string(keys[i]).uint32(18).string(message.platformConfig[keys[i]]).ldelim();
                    return writer;
                };

                EmulatorStatus.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.EmulatorStatus(), key, value;
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.version = reader.string();
                                break;
                            }
                        case 2: {
                                message.uptime = reader.uint64();
                                break;
                            }
                        case 3: {
                                message.booted = reader.bool();
                                break;
                            }
                        case 4: {
                                message.vmConfig = $root.android.emulation.control.VmConfiguration.decode(reader, reader.uint32());
                                break;
                            }
                        case 5: {
                                message.hardwareConfig = $root.android.emulation.control.EntryList.decode(reader, reader.uint32());
                                break;
                            }
                        case 6: {
                                message.heartbeat = reader.uint64();
                                break;
                            }
                        case 7: {
                                if (message.guestConfig === $util.emptyObject)
                                    message.guestConfig = {};
                                let end2 = reader.uint32() + reader.pos;
                                key = "";
                                value = "";
                                while (reader.pos < end2) {
                                    let tag2 = reader.uint32();
                                    switch (tag2 >>> 3) {
                                    case 1:
                                        key = reader.string();
                                        break;
                                    case 2:
                                        value = reader.string();
                                        break;
                                    default:
                                        reader.skipType(tag2 & 7);
                                        break;
                                    }
                                }
                                message.guestConfig[key] = value;
                                break;
                            }
                        case 8: {
                                if (message.platformConfig === $util.emptyObject)
                                    message.platformConfig = {};
                                let end2 = reader.uint32() + reader.pos;
                                key = "";
                                value = "";
                                while (reader.pos < end2) {
                                    let tag2 = reader.uint32();
                                    switch (tag2 >>> 3) {
                                    case 1:
                                        key = reader.string();
                                        break;
                                    case 2:
                                        value = reader.string();
                                        break;
                                    default:
                                        reader.skipType(tag2 & 7);
                                        break;
                                    }
                                }
                                message.platformConfig[key] = value;
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                EmulatorStatus.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.EmulatorStatus";
                };

                return EmulatorStatus;
            })();

            control.AudioFormat = (function() {

                function AudioFormat(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                AudioFormat.prototype.samplingRate = $util.Long ? $util.Long.fromBits(0,0,true) : 0;
                AudioFormat.prototype.channels = 0;
                AudioFormat.prototype.format = 0;
                AudioFormat.prototype.mode = 0;

                AudioFormat.create = function create(properties) {
                    return new AudioFormat(properties);
                };

                AudioFormat.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.samplingRate != null && Object.hasOwnProperty.call(message, "samplingRate"))
                        writer.uint32(8).uint64(message.samplingRate);
                    if (message.channels != null && Object.hasOwnProperty.call(message, "channels"))
                        writer.uint32(16).int32(message.channels);
                    if (message.format != null && Object.hasOwnProperty.call(message, "format"))
                        writer.uint32(24).int32(message.format);
                    if (message.mode != null && Object.hasOwnProperty.call(message, "mode"))
                        writer.uint32(32).int32(message.mode);
                    return writer;
                };

                AudioFormat.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.AudioFormat();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.samplingRate = reader.uint64();
                                break;
                            }
                        case 2: {
                                message.channels = reader.int32();
                                break;
                            }
                        case 3: {
                                message.format = reader.int32();
                                break;
                            }
                        case 4: {
                                message.mode = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                AudioFormat.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.AudioFormat";
                };

                AudioFormat.SampleFormat = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "AUD_FMT_U8"] = 0;
                    values[valuesById[1] = "AUD_FMT_S16"] = 1;
                    return values;
                })();

                AudioFormat.Channels = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "Mono"] = 0;
                    values[valuesById[1] = "Stereo"] = 1;
                    return values;
                })();

                AudioFormat.DeliveryMode = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "MODE_UNSPECIFIED"] = 0;
                    values[valuesById[1] = "MODE_REAL_TIME"] = 1;
                    return values;
                })();

                return AudioFormat;
            })();

            control.AudioPacket = (function() {

                function AudioPacket(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                AudioPacket.prototype.format = null;
                AudioPacket.prototype.timestamp = $util.Long ? $util.Long.fromBits(0,0,true) : 0;
                AudioPacket.prototype.audio = $util.newBuffer([]);

                AudioPacket.create = function create(properties) {
                    return new AudioPacket(properties);
                };

                AudioPacket.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.format != null && Object.hasOwnProperty.call(message, "format"))
                        $root.android.emulation.control.AudioFormat.encode(message.format, writer.uint32(10).fork()).ldelim();
                    if (message.timestamp != null && Object.hasOwnProperty.call(message, "timestamp"))
                        writer.uint32(16).uint64(message.timestamp);
                    if (message.audio != null && Object.hasOwnProperty.call(message, "audio"))
                        writer.uint32(26).bytes(message.audio);
                    return writer;
                };

                AudioPacket.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.AudioPacket();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.format = $root.android.emulation.control.AudioFormat.decode(reader, reader.uint32());
                                break;
                            }
                        case 2: {
                                message.timestamp = reader.uint64();
                                break;
                            }
                        case 3: {
                                message.audio = reader.bytes();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                AudioPacket.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.AudioPacket";
                };

                return AudioPacket;
            })();

            control.MicrophoneState = (function() {

                function MicrophoneState(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                MicrophoneState.prototype.realAudioEnabled = false;

                MicrophoneState.create = function create(properties) {
                    return new MicrophoneState(properties);
                };

                MicrophoneState.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.realAudioEnabled != null && Object.hasOwnProperty.call(message, "realAudioEnabled"))
                        writer.uint32(8).bool(message.realAudioEnabled);
                    return writer;
                };

                MicrophoneState.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.MicrophoneState();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.realAudioEnabled = reader.bool();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                MicrophoneState.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.MicrophoneState";
                };

                return MicrophoneState;
            })();

            control.SmsMessage = (function() {

                function SmsMessage(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                SmsMessage.prototype.srcAddress = "";
                SmsMessage.prototype.text = "";

                SmsMessage.create = function create(properties) {
                    return new SmsMessage(properties);
                };

                SmsMessage.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.srcAddress != null && Object.hasOwnProperty.call(message, "srcAddress"))
                        writer.uint32(10).string(message.srcAddress);
                    if (message.text != null && Object.hasOwnProperty.call(message, "text"))
                        writer.uint32(18).string(message.text);
                    return writer;
                };

                SmsMessage.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.SmsMessage();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.srcAddress = reader.string();
                                break;
                            }
                        case 2: {
                                message.text = reader.string();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                SmsMessage.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.SmsMessage";
                };

                return SmsMessage;
            })();

            control.DisplayConfiguration = (function() {

                function DisplayConfiguration(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                DisplayConfiguration.prototype.width = 0;
                DisplayConfiguration.prototype.height = 0;
                DisplayConfiguration.prototype.dpi = 0;
                DisplayConfiguration.prototype.flags = 0;
                DisplayConfiguration.prototype.display = 0;

                DisplayConfiguration.create = function create(properties) {
                    return new DisplayConfiguration(properties);
                };

                DisplayConfiguration.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.width != null && Object.hasOwnProperty.call(message, "width"))
                        writer.uint32(8).uint32(message.width);
                    if (message.height != null && Object.hasOwnProperty.call(message, "height"))
                        writer.uint32(16).uint32(message.height);
                    if (message.dpi != null && Object.hasOwnProperty.call(message, "dpi"))
                        writer.uint32(24).uint32(message.dpi);
                    if (message.flags != null && Object.hasOwnProperty.call(message, "flags"))
                        writer.uint32(32).uint32(message.flags);
                    if (message.display != null && Object.hasOwnProperty.call(message, "display"))
                        writer.uint32(40).uint32(message.display);
                    return writer;
                };

                DisplayConfiguration.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.DisplayConfiguration();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.width = reader.uint32();
                                break;
                            }
                        case 2: {
                                message.height = reader.uint32();
                                break;
                            }
                        case 3: {
                                message.dpi = reader.uint32();
                                break;
                            }
                        case 4: {
                                message.flags = reader.uint32();
                                break;
                            }
                        case 5: {
                                message.display = reader.uint32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                DisplayConfiguration.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.DisplayConfiguration";
                };

                DisplayConfiguration.DisplayFlags = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "DISPLAYFLAGS_UNSPECIFIED"] = 0;
                    values[valuesById[1] = "VIRTUAL_DISPLAY_FLAG_PUBLIC"] = 1;
                    values[valuesById[2] = "VIRTUAL_DISPLAY_FLAG_PRESENTATION"] = 2;
                    values[valuesById[4] = "VIRTUAL_DISPLAY_FLAG_SECURE"] = 4;
                    values[valuesById[8] = "VIRTUAL_DISPLAY_FLAG_OWN_CONTENT_ONLY"] = 8;
                    values[valuesById[16] = "VIRTUAL_DISPLAY_FLAG_AUTO_MIRROR"] = 16;
                    return values;
                })();

                return DisplayConfiguration;
            })();

            control.DisplayPowerModeNotification = (function() {

                function DisplayPowerModeNotification(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                DisplayPowerModeNotification.prototype.display = 0;
                DisplayPowerModeNotification.prototype.powerMode = 0;

                DisplayPowerModeNotification.create = function create(properties) {
                    return new DisplayPowerModeNotification(properties);
                };

                DisplayPowerModeNotification.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.display != null && Object.hasOwnProperty.call(message, "display"))
                        writer.uint32(8).uint32(message.display);
                    if (message.powerMode != null && Object.hasOwnProperty.call(message, "powerMode"))
                        writer.uint32(16).int32(message.powerMode);
                    return writer;
                };

                DisplayPowerModeNotification.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.DisplayPowerModeNotification();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.display = reader.uint32();
                                break;
                            }
                        case 2: {
                                message.powerMode = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                DisplayPowerModeNotification.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.DisplayPowerModeNotification";
                };

                DisplayPowerModeNotification.PowerMode = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "OFF"] = 0;
                    values[valuesById[1] = "DOZE"] = 1;
                    values[valuesById[2] = "ON"] = 2;
                    values[valuesById[3] = "DOZE_SUSPEND"] = 3;
                    values[valuesById[4] = "ON_SUSPEND"] = 4;
                    return values;
                })();

                return DisplayPowerModeNotification;
            })();

            control.DisplayConfigurations = (function() {

                function DisplayConfigurations(properties) {
                    this.displays = [];
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                DisplayConfigurations.prototype.displays = $util.emptyArray;
                DisplayConfigurations.prototype.userConfigurable = 0;
                DisplayConfigurations.prototype.maxDisplays = 0;

                DisplayConfigurations.create = function create(properties) {
                    return new DisplayConfigurations(properties);
                };

                DisplayConfigurations.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.displays != null && message.displays.length)
                        for (let i = 0; i < message.displays.length; ++i)
                            $root.android.emulation.control.DisplayConfiguration.encode(message.displays[i], writer.uint32(10).fork()).ldelim();
                    if (message.userConfigurable != null && Object.hasOwnProperty.call(message, "userConfigurable"))
                        writer.uint32(16).uint32(message.userConfigurable);
                    if (message.maxDisplays != null && Object.hasOwnProperty.call(message, "maxDisplays"))
                        writer.uint32(24).uint32(message.maxDisplays);
                    return writer;
                };

                DisplayConfigurations.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.DisplayConfigurations();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                if (!(message.displays && message.displays.length))
                                    message.displays = [];
                                message.displays.push($root.android.emulation.control.DisplayConfiguration.decode(reader, reader.uint32()));
                                break;
                            }
                        case 2: {
                                message.userConfigurable = reader.uint32();
                                break;
                            }
                        case 3: {
                                message.maxDisplays = reader.uint32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                DisplayConfigurations.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.DisplayConfigurations";
                };

                return DisplayConfigurations;
            })();

            control.Notification = (function() {

                function Notification(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                Notification.prototype.cameraNotification = null;
                Notification.prototype.displayConfigurationsChangedNotification = null;
                Notification.prototype.posture = null;
                Notification.prototype.booted = null;
                Notification.prototype.brightness = null;
                Notification.prototype.textViewFocus = null;
                Notification.prototype.xrOptions = null;
                Notification.prototype.microphoneState = null;
                Notification.prototype.ledIndicator = null;
                Notification.prototype.displayPowerMode = null;

                let $oneOfFields;

                Object.defineProperty(Notification.prototype, "type", {
                    get: $util.oneOfGetter($oneOfFields = ["cameraNotification", "displayConfigurationsChangedNotification", "posture", "booted", "brightness", "textViewFocus", "xrOptions", "microphoneState", "ledIndicator", "displayPowerMode"]),
                    set: $util.oneOfSetter($oneOfFields)
                });

                Notification.create = function create(properties) {
                    return new Notification(properties);
                };

                Notification.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.cameraNotification != null && Object.hasOwnProperty.call(message, "cameraNotification"))
                        $root.android.emulation.control.CameraNotification.encode(message.cameraNotification, writer.uint32(18).fork()).ldelim();
                    if (message.displayConfigurationsChangedNotification != null && Object.hasOwnProperty.call(message, "displayConfigurationsChangedNotification"))
                        $root.android.emulation.control.DisplayConfigurationsChangedNotification.encode(message.displayConfigurationsChangedNotification, writer.uint32(26).fork()).ldelim();
                    if (message.posture != null && Object.hasOwnProperty.call(message, "posture"))
                        $root.android.emulation.control.Posture.encode(message.posture, writer.uint32(34).fork()).ldelim();
                    if (message.booted != null && Object.hasOwnProperty.call(message, "booted"))
                        $root.android.emulation.control.BootCompletedNotification.encode(message.booted, writer.uint32(42).fork()).ldelim();
                    if (message.brightness != null && Object.hasOwnProperty.call(message, "brightness"))
                        $root.android.emulation.control.BrightnessValue.encode(message.brightness, writer.uint32(50).fork()).ldelim();
                    if (message.textViewFocus != null && Object.hasOwnProperty.call(message, "textViewFocus"))
                        $root.android.emulation.control.TextViewFocus.encode(message.textViewFocus, writer.uint32(58).fork()).ldelim();
                    if (message.xrOptions != null && Object.hasOwnProperty.call(message, "xrOptions"))
                        $root.android.emulation.control.XrOptions.encode(message.xrOptions, writer.uint32(66).fork()).ldelim();
                    if (message.microphoneState != null && Object.hasOwnProperty.call(message, "microphoneState"))
                        $root.android.emulation.control.MicrophoneState.encode(message.microphoneState, writer.uint32(74).fork()).ldelim();
                    if (message.ledIndicator != null && Object.hasOwnProperty.call(message, "ledIndicator"))
                        $root.android.emulation.control.LedIndicator.encode(message.ledIndicator, writer.uint32(82).fork()).ldelim();
                    if (message.displayPowerMode != null && Object.hasOwnProperty.call(message, "displayPowerMode"))
                        $root.android.emulation.control.DisplayPowerModeNotification.encode(message.displayPowerMode, writer.uint32(90).fork()).ldelim();
                    return writer;
                };

                Notification.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.Notification();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 2: {
                                message.cameraNotification = $root.android.emulation.control.CameraNotification.decode(reader, reader.uint32());
                                break;
                            }
                        case 3: {
                                message.displayConfigurationsChangedNotification = $root.android.emulation.control.DisplayConfigurationsChangedNotification.decode(reader, reader.uint32());
                                break;
                            }
                        case 4: {
                                message.posture = $root.android.emulation.control.Posture.decode(reader, reader.uint32());
                                break;
                            }
                        case 5: {
                                message.booted = $root.android.emulation.control.BootCompletedNotification.decode(reader, reader.uint32());
                                break;
                            }
                        case 6: {
                                message.brightness = $root.android.emulation.control.BrightnessValue.decode(reader, reader.uint32());
                                break;
                            }
                        case 7: {
                                message.textViewFocus = $root.android.emulation.control.TextViewFocus.decode(reader, reader.uint32());
                                break;
                            }
                        case 8: {
                                message.xrOptions = $root.android.emulation.control.XrOptions.decode(reader, reader.uint32());
                                break;
                            }
                        case 9: {
                                message.microphoneState = $root.android.emulation.control.MicrophoneState.decode(reader, reader.uint32());
                                break;
                            }
                        case 10: {
                                message.ledIndicator = $root.android.emulation.control.LedIndicator.decode(reader, reader.uint32());
                                break;
                            }
                        case 11: {
                                message.displayPowerMode = $root.android.emulation.control.DisplayPowerModeNotification.decode(reader, reader.uint32());
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                Notification.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.Notification";
                };

                return Notification;
            })();

            control.BootCompletedNotification = (function() {

                function BootCompletedNotification(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                BootCompletedNotification.prototype.time = 0;

                BootCompletedNotification.create = function create(properties) {
                    return new BootCompletedNotification(properties);
                };

                BootCompletedNotification.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.time != null && Object.hasOwnProperty.call(message, "time"))
                        writer.uint32(8).int32(message.time);
                    return writer;
                };

                BootCompletedNotification.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.BootCompletedNotification();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.time = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                BootCompletedNotification.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.BootCompletedNotification";
                };

                return BootCompletedNotification;
            })();

            control.CameraNotification = (function() {

                function CameraNotification(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                CameraNotification.prototype.active = false;
                CameraNotification.prototype.display = 0;

                CameraNotification.create = function create(properties) {
                    return new CameraNotification(properties);
                };

                CameraNotification.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.active != null && Object.hasOwnProperty.call(message, "active"))
                        writer.uint32(8).bool(message.active);
                    if (message.display != null && Object.hasOwnProperty.call(message, "display"))
                        writer.uint32(16).int32(message.display);
                    return writer;
                };

                CameraNotification.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.CameraNotification();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.active = reader.bool();
                                break;
                            }
                        case 2: {
                                message.display = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                CameraNotification.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.CameraNotification";
                };

                return CameraNotification;
            })();

            control.TextViewFocus = (function() {

                function TextViewFocus(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                TextViewFocus.prototype.textViewHasFocus = false;
                TextViewFocus.prototype.display = 0;

                TextViewFocus.create = function create(properties) {
                    return new TextViewFocus(properties);
                };

                TextViewFocus.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.textViewHasFocus != null && Object.hasOwnProperty.call(message, "textViewHasFocus"))
                        writer.uint32(8).bool(message.textViewHasFocus);
                    if (message.display != null && Object.hasOwnProperty.call(message, "display"))
                        writer.uint32(16).int32(message.display);
                    return writer;
                };

                TextViewFocus.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.TextViewFocus();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.textViewHasFocus = reader.bool();
                                break;
                            }
                        case 2: {
                                message.display = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                TextViewFocus.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.TextViewFocus";
                };

                return TextViewFocus;
            })();

            control.DisplayConfigurationsChangedNotification = (function() {

                function DisplayConfigurationsChangedNotification(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                DisplayConfigurationsChangedNotification.prototype.displayConfigurations = null;

                DisplayConfigurationsChangedNotification.create = function create(properties) {
                    return new DisplayConfigurationsChangedNotification(properties);
                };

                DisplayConfigurationsChangedNotification.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.displayConfigurations != null && Object.hasOwnProperty.call(message, "displayConfigurations"))
                        $root.android.emulation.control.DisplayConfigurations.encode(message.displayConfigurations, writer.uint32(10).fork()).ldelim();
                    return writer;
                };

                DisplayConfigurationsChangedNotification.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.DisplayConfigurationsChangedNotification();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.displayConfigurations = $root.android.emulation.control.DisplayConfigurations.decode(reader, reader.uint32());
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                DisplayConfigurationsChangedNotification.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.DisplayConfigurationsChangedNotification";
                };

                return DisplayConfigurationsChangedNotification;
            })();

            control.RotationRadian = (function() {

                function RotationRadian(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                RotationRadian.prototype.x = 0;
                RotationRadian.prototype.y = 0;
                RotationRadian.prototype.z = 0;

                RotationRadian.create = function create(properties) {
                    return new RotationRadian(properties);
                };

                RotationRadian.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.x != null && Object.hasOwnProperty.call(message, "x"))
                        writer.uint32(13).float(message.x);
                    if (message.y != null && Object.hasOwnProperty.call(message, "y"))
                        writer.uint32(21).float(message.y);
                    if (message.z != null && Object.hasOwnProperty.call(message, "z"))
                        writer.uint32(29).float(message.z);
                    return writer;
                };

                RotationRadian.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.RotationRadian();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.x = reader.float();
                                break;
                            }
                        case 2: {
                                message.y = reader.float();
                                break;
                            }
                        case 3: {
                                message.z = reader.float();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                RotationRadian.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.RotationRadian";
                };

                return RotationRadian;
            })();

            control.Translation = (function() {

                function Translation(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                Translation.prototype.deltaX = 0;
                Translation.prototype.deltaY = 0;
                Translation.prototype.deltaZ = 0;

                Translation.create = function create(properties) {
                    return new Translation(properties);
                };

                Translation.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.deltaX != null && Object.hasOwnProperty.call(message, "deltaX"))
                        writer.uint32(13).float(message.deltaX);
                    if (message.deltaY != null && Object.hasOwnProperty.call(message, "deltaY"))
                        writer.uint32(21).float(message.deltaY);
                    if (message.deltaZ != null && Object.hasOwnProperty.call(message, "deltaZ"))
                        writer.uint32(29).float(message.deltaZ);
                    return writer;
                };

                Translation.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.Translation();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.deltaX = reader.float();
                                break;
                            }
                        case 2: {
                                message.deltaY = reader.float();
                                break;
                            }
                        case 3: {
                                message.deltaZ = reader.float();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                Translation.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.Translation";
                };

                return Translation;
            })();

            control.AngularVelocity = (function() {

                function AngularVelocity(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                AngularVelocity.prototype.omegaX = 0;
                AngularVelocity.prototype.omegaY = 0;
                AngularVelocity.prototype.omegaZ = 0;

                AngularVelocity.create = function create(properties) {
                    return new AngularVelocity(properties);
                };

                AngularVelocity.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.omegaX != null && Object.hasOwnProperty.call(message, "omegaX"))
                        writer.uint32(13).float(message.omegaX);
                    if (message.omegaY != null && Object.hasOwnProperty.call(message, "omegaY"))
                        writer.uint32(21).float(message.omegaY);
                    if (message.omegaZ != null && Object.hasOwnProperty.call(message, "omegaZ"))
                        writer.uint32(29).float(message.omegaZ);
                    return writer;
                };

                AngularVelocity.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.AngularVelocity();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.omegaX = reader.float();
                                break;
                            }
                        case 2: {
                                message.omegaY = reader.float();
                                break;
                            }
                        case 3: {
                                message.omegaZ = reader.float();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                AngularVelocity.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.AngularVelocity";
                };

                return AngularVelocity;
            })();

            control.Velocity = (function() {

                function Velocity(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                Velocity.prototype.x = 0;
                Velocity.prototype.y = 0;
                Velocity.prototype.z = 0;

                Velocity.create = function create(properties) {
                    return new Velocity(properties);
                };

                Velocity.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.x != null && Object.hasOwnProperty.call(message, "x"))
                        writer.uint32(13).float(message.x);
                    if (message.y != null && Object.hasOwnProperty.call(message, "y"))
                        writer.uint32(21).float(message.y);
                    if (message.z != null && Object.hasOwnProperty.call(message, "z"))
                        writer.uint32(29).float(message.z);
                    return writer;
                };

                Velocity.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.Velocity();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.x = reader.float();
                                break;
                            }
                        case 2: {
                                message.y = reader.float();
                                break;
                            }
                        case 3: {
                                message.z = reader.float();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                Velocity.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.Velocity";
                };

                return Velocity;
            })();

            control.Posture = (function() {

                function Posture(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                Posture.prototype.value = 0;

                Posture.create = function create(properties) {
                    return new Posture(properties);
                };

                Posture.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.value != null && Object.hasOwnProperty.call(message, "value"))
                        writer.uint32(24).int32(message.value);
                    return writer;
                };

                Posture.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.Posture();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 3: {
                                message.value = reader.int32();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                Posture.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.Posture";
                };

                Posture.PostureValue = (function() {
                    const valuesById = {}, values = Object.create(valuesById);
                    values[valuesById[0] = "POSTURE_UNKNOWN"] = 0;
                    values[valuesById[1] = "POSTURE_CLOSED"] = 1;
                    values[valuesById[2] = "POSTURE_HALF_OPENED"] = 2;
                    values[valuesById[3] = "POSTURE_OPENED"] = 3;
                    values[valuesById[4] = "POSTURE_FLIPPED"] = 4;
                    values[valuesById[5] = "POSTURE_TENT"] = 5;
                    values[valuesById[6] = "POSTURE_MAX"] = 6;
                    return values;
                })();

                return Posture;
            })();

            control.PhoneNumber = (function() {

                function PhoneNumber(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                PhoneNumber.prototype.number = "";

                PhoneNumber.create = function create(properties) {
                    return new PhoneNumber(properties);
                };

                PhoneNumber.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.number != null && Object.hasOwnProperty.call(message, "number"))
                        writer.uint32(10).string(message.number);
                    return writer;
                };

                PhoneNumber.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.PhoneNumber();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.number = reader.string();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                PhoneNumber.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.PhoneNumber";
                };

                return PhoneNumber;
            })();

            control.Environment = (function() {

                function Environment(properties) {
                    this.environment = {};
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                Environment.prototype.environment = $util.emptyObject;

                Environment.create = function create(properties) {
                    return new Environment(properties);
                };

                Environment.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.environment != null && Object.hasOwnProperty.call(message, "environment"))
                        for (let keys = Object.keys(message.environment), i = 0; i < keys.length; ++i)
                            writer.uint32(10).fork().uint32(10).string(keys[i]).uint32(18).string(message.environment[keys[i]]).ldelim();
                    return writer;
                };

                Environment.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.Environment(), key, value;
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                if (message.environment === $util.emptyObject)
                                    message.environment = {};
                                let end2 = reader.uint32() + reader.pos;
                                key = "";
                                value = "";
                                while (reader.pos < end2) {
                                    let tag2 = reader.uint32();
                                    switch (tag2 >>> 3) {
                                    case 1:
                                        key = reader.string();
                                        break;
                                    case 2:
                                        value = reader.string();
                                        break;
                                    default:
                                        reader.skipType(tag2 & 7);
                                        break;
                                    }
                                }
                                message.environment[key] = value;
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                Environment.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.Environment";
                };

                return Environment;
            })();

            control.Camera = (function() {

                function Camera(properties) {
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                Camera.prototype.displayName = "";
                Camera.prototype.id = "";

                Camera.create = function create(properties) {
                    return new Camera(properties);
                };

                Camera.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.displayName != null && Object.hasOwnProperty.call(message, "displayName"))
                        writer.uint32(10).string(message.displayName);
                    if (message.id != null && Object.hasOwnProperty.call(message, "id"))
                        writer.uint32(18).string(message.id);
                    return writer;
                };

                Camera.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.Camera();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                message.displayName = reader.string();
                                break;
                            }
                        case 2: {
                                message.id = reader.string();
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                Camera.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.Camera";
                };

                return Camera;
            })();

            control.CameraList = (function() {

                function CameraList(properties) {
                    this.cameras = [];
                    if (properties)
                        for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                            if (properties[keys[i]] != null)
                                this[keys[i]] = properties[keys[i]];
                }

                CameraList.prototype.cameras = $util.emptyArray;

                CameraList.create = function create(properties) {
                    return new CameraList(properties);
                };

                CameraList.encode = function encode(message, writer) {
                    if (!writer)
                        writer = $Writer.create();
                    if (message.cameras != null && message.cameras.length)
                        for (let i = 0; i < message.cameras.length; ++i)
                            $root.android.emulation.control.Camera.encode(message.cameras[i], writer.uint32(10).fork()).ldelim();
                    return writer;
                };

                CameraList.decode = function decode(reader, length, error) {
                    if (!(reader instanceof $Reader))
                        reader = $Reader.create(reader);
                    let end = length === undefined ? reader.len : reader.pos + length, message = new $root.android.emulation.control.CameraList();
                    while (reader.pos < end) {
                        let tag = reader.uint32();
                        if (tag === error)
                            break;
                        switch (tag >>> 3) {
                        case 1: {
                                if (!(message.cameras && message.cameras.length))
                                    message.cameras = [];
                                message.cameras.push($root.android.emulation.control.Camera.decode(reader, reader.uint32()));
                                break;
                            }
                        default:
                            reader.skipType(tag & 7);
                            break;
                        }
                    }
                    return message;
                };

                CameraList.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                    if (typeUrlPrefix === undefined) {
                        typeUrlPrefix = "type.googleapis.com";
                    }
                    return typeUrlPrefix + "/android.emulation.control.CameraList";
                };

                return CameraList;
            })();

            return control;
        })();

        return emulation;
    })();

    return android;
})();

export const google = $root.google = (() => {

    const google = {};

    google.protobuf = (function() {

        const protobuf = {};

        protobuf.Empty = (function() {

            function Empty(properties) {
                if (properties)
                    for (let keys = Object.keys(properties), i = 0; i < keys.length; ++i)
                        if (properties[keys[i]] != null)
                            this[keys[i]] = properties[keys[i]];
            }

            Empty.create = function create(properties) {
                return new Empty(properties);
            };

            Empty.encode = function encode(message, writer) {
                if (!writer)
                    writer = $Writer.create();
                return writer;
            };

            Empty.decode = function decode(reader, length, error) {
                if (!(reader instanceof $Reader))
                    reader = $Reader.create(reader);
                let end = length === undefined ? reader.len : reader.pos + length, message = new $root.google.protobuf.Empty();
                while (reader.pos < end) {
                    let tag = reader.uint32();
                    if (tag === error)
                        break;
                    switch (tag >>> 3) {
                    default:
                        reader.skipType(tag & 7);
                        break;
                    }
                }
                return message;
            };

            Empty.getTypeUrl = function getTypeUrl(typeUrlPrefix) {
                if (typeUrlPrefix === undefined) {
                    typeUrlPrefix = "type.googleapis.com";
                }
                return typeUrlPrefix + "/google.protobuf.Empty";
            };

            return Empty;
        })();

        return protobuf;
    })();

    return google;
})();

export { $root as default };
